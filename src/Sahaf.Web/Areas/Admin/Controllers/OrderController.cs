using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Models;
using Sahaf.Models.ViewModels;
using Sahaf.Utility;
using Sahaf.Web.Infrastructure;
using Sahaf.Web.Localization;
using Stripe;

namespace Sahaf.Web.Areas.Admin.Controllers;

// Staff (admin, employee) see and manage every order; everyone else only
// sees their own. Every action checks this on the server.
[Area("Admin")]
[Authorize]
public class OrderController(
    IUnitOfWork unitOfWork,
    PaymentService payments,
    OrderEmails emails,
    IStringLocalizer<SharedResource> t,
    ILogger<OrderController> logger) : Controller
{
    private const int PageSize = 20;
    private const string StaffRoles = SD.Role_Admin + "," + SD.Role_Employee;

    public IActionResult Index(string status = "all", int page = 1)
    {
        var query = unitOfWork.OrderHeader.Query("ApplicationUser");
        if (!User.IsStaff())
        {
            var userId = User.UserId();
            query = query.Where(o => o.ApplicationUserId == userId);
        }
        query = status switch
        {
            "pending" => query.Where(o => o.PaymentStatus == SD.PaymentStatusDelayedPayment || o.OrderStatus == SD.StatusPending),
            "approved" => query.Where(o => o.OrderStatus == SD.StatusApproved),
            "processing" => query.Where(o => o.OrderStatus == SD.StatusInProcess),
            "shipped" => query.Where(o => o.OrderStatus == SD.StatusShipped),
            "cancelled" => query.Where(o => o.OrderStatus == SD.StatusCancelled),
            _ => query,
        };

        var total = query.Count();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);
        ViewBag.Status = status;
        return View(new AdminListVM<OrderHeader>
        {
            Items = query.OrderByDescending(o => o.OrderDate).Skip((page - 1) * PageSize).Take(PageSize).ToList(),
            Page = page,
            TotalPages = totalPages,
            TotalCount = total,
        });
    }

    public IActionResult Details(Guid id)
    {
        var order = VisibleOrder(id);
        if (order == null) return NotFound();
        return View(new OrderVM
        {
            OrderHeader = order,
            OrderDetails = unitOfWork.OrderDetail.Query("Product").Where(d => d.OrderHeaderId == id).ToList(),
        });
    }

    // Shipping details and carrier info; only these fields are copied.
    [HttpPost]
    [Authorize(Roles = StaffRoles)]
    public IActionResult UpdateDetails(OrderVM vm)
    {
        var input = vm.OrderHeader;
        var order = unitOfWork.OrderHeader.Get(o => o.Id == input.Id, tracked: true);
        if (order == null) return NotFound();
        if (!ModelState.IsValid)
        {
            TempData["error"] = t["order.invalidDetails"].Value;
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
        order.Name = input.Name.Trim();
        order.PhoneNumber = input.PhoneNumber.Trim();
        order.StreetAddress = input.StreetAddress.Trim();
        order.City = input.City.Trim();
        order.State = input.State.Trim();
        order.PostalCode = input.PostalCode.Trim();
        order.Carrier = input.Carrier?.Trim();
        order.TrackingNumber = input.TrackingNumber?.Trim();
        unitOfWork.Save();
        TempData["success"] = t["admin.updated"].Value;
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpPost]
    [Authorize(Roles = StaffRoles)]
    public IActionResult StartProcessing(Guid id)
    {
        var order = unitOfWork.OrderHeader.Get(o => o.Id == id);
        if (order == null) return NotFound();
        if (!OrderWorkflow.CanStartProcessing(order.OrderStatus)) return InvalidTransition(id);

        unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusInProcess);
        unitOfWork.Save();
        TempData["success"] = t["order.processingStarted"].Value;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = StaffRoles)]
    public async Task<IActionResult> Ship(Guid id, string? carrier, string? trackingNumber)
    {
        var order = unitOfWork.OrderHeader.Get(o => o.Id == id, "ApplicationUser", tracked: true);
        if (order == null) return NotFound();
        if (!OrderWorkflow.CanShip(order.OrderStatus)) return InvalidTransition(id);

        carrier = carrier?.Trim();
        trackingNumber = trackingNumber?.Trim();
        if (string.IsNullOrEmpty(carrier) || string.IsNullOrEmpty(trackingNumber) || carrier.Length > 40 || trackingNumber.Length > 40)
        {
            TempData["error"] = t["order.shippingRequired"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        order.Carrier = carrier;
        order.TrackingNumber = trackingNumber;
        order.OrderStatus = SD.StatusShipped;
        order.ShippingDate = DateTime.UtcNow;
        unitOfWork.Save();

        if (order.ApplicationUser.Email is { } email)
            await emails.SendShippedAsync(order, email, OrderUrl(id));
        TempData["success"] = t["order.shipped"].Value;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Roles = StaffRoles)]
    public IActionResult Cancel(Guid id)
    {
        var order = unitOfWork.OrderHeader.Get(o => o.Id == id);
        if (order == null) return NotFound();
        if (!OrderWorkflow.CanCancel(order.OrderStatus)) return InvalidTransition(id);

        if (OrderWorkflow.NeedsRefund(order.PaymentStatus))
        {
            try
            {
                payments.Refund(order);
            }
            catch (StripeException ex)
            {
                logger.LogError(ex, "Refund failed for order {Order}", id);
                TempData["error"] = t["order.refundFailed"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }
            unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusCancelled, SD.StatusRefunded);
        }
        else
        {
            unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusCancelled, SD.StatusCancelled);
        }
        unitOfWork.Save();
        TempData["success"] = t["order.cancelled"].Value;
        return RedirectToAction(nameof(Details), new { id });
    }

    // A company user pays a delayed-payment order through Stripe.
    [HttpPost]
    public IActionResult PayNow(Guid id)
    {
        var order = unitOfWork.OrderHeader.Get(o => o.Id == id);
        if (order == null || order.ApplicationUserId != User.UserId()) return NotFound();
        if (!OrderWorkflow.CanPayLater(order.OrderStatus, order.PaymentStatus)) return InvalidTransition(id);
        if (!payments.IsConfigured)
        {
            TempData["error"] = t["pay.notConfigured"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        var english = Culture.IsEnglish;
        var lines = unitOfWork.OrderDetail.Query("Product").Where(d => d.OrderHeaderId == id).ToList()
            .Select(d => new CheckoutLine(d.Product.LocalTitle(english), d.Count, d.Price));
        try
        {
            var session = payments.CreateCheckout(order, lines, english,
                Url.Action(nameof(PaymentConfirmation), "Order", new { area = "Admin", id }, Request.Scheme)!,
                Url.Action(nameof(Details), "Order", new { area = "Admin", id }, Request.Scheme)!,
                User.Identity?.Name);
            unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
            unitOfWork.Save();
            Response.Headers.Location = session.Url;
            return new StatusCodeResult(StatusCodes.Status303SeeOther);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe checkout could not be created for order {Order}", id);
            TempData["error"] = t["pay.failed"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    public async Task<IActionResult> PaymentConfirmation(Guid id)
    {
        var order = unitOfWork.OrderHeader.Get(o => o.Id == id);
        if (order == null || order.ApplicationUserId != User.UserId()) return NotFound();
        try
        {
            if (await payments.SyncAsync(order, OrderUrl(id)))
                TempData["success"] = t["order.paid"].Value;
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe session check failed for order {Order}", id);
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private OrderHeader? VisibleOrder(Guid id)
    {
        var order = unitOfWork.OrderHeader.Get(o => o.Id == id, "ApplicationUser");
        if (order == null) return null;
        return User.IsStaff() || order.ApplicationUserId == User.UserId() ? order : null;
    }

    private IActionResult InvalidTransition(Guid id)
    {
        TempData["error"] = t["order.invalidTransition"].Value;
        return RedirectToAction(nameof(Details), new { id });
    }

    private string OrderUrl(Guid id) =>
        Url.Action(nameof(Details), "Order", new { area = "Admin", id }, Request.Scheme)!;
}
