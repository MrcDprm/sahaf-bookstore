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

namespace Sahaf.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class CartController(
    IUnitOfWork unitOfWork,
    PaymentService payments,
    IStringLocalizer<SharedResource> t,
    ILogger<CartController> logger) : Controller
{
    public IActionResult Index() => View(BuildCart(User.UserId()));

    public IActionResult Summary()
    {
        var userId = User.UserId();
        var vm = BuildCart(userId);
        if (vm.ShoppingCartList.Count == 0) return RedirectToAction(nameof(Index));

        var user = unitOfWork.ApplicationUser.Get(u => u.Id == userId)!;
        vm.OrderHeader.Name = user.Name;
        vm.OrderHeader.PhoneNumber = user.PhoneNumber ?? "";
        vm.OrderHeader.StreetAddress = user.StreetAddress ?? "";
        vm.OrderHeader.City = user.City ?? "";
        vm.OrderHeader.State = user.State ?? "";
        vm.OrderHeader.PostalCode = user.PostalCode ?? "";
        return View(vm);
    }

    [HttpPost]
    [ActionName("Summary")]
    public IActionResult SummaryPost([Bind(Prefix = "OrderHeader")] OrderHeader input)
    {
        var userId = User.UserId();
        var vm = BuildCart(userId);
        if (vm.ShoppingCartList.Count == 0) return RedirectToAction(nameof(Index));
        if (!ModelState.IsValid)
        {
            input.OrderTotal = vm.OrderHeader.OrderTotal;
            vm.OrderHeader = input;
            return View(vm);
        }
        if (!vm.IsCompanyUser && !payments.IsConfigured)
        {
            TempData["error"] = t["pay.notConfigured"].Value;
            return RedirectToAction(nameof(Index));
        }

        // Only the shipping fields come from the form; owner, total and
        // statuses are always set here on the server.
        var order = new OrderHeader
        {
            ApplicationUserId = userId,
            OrderDate = DateTime.UtcNow,
            OrderTotal = vm.OrderHeader.OrderTotal,
            Name = input.Name.Trim(),
            PhoneNumber = input.PhoneNumber.Trim(),
            StreetAddress = input.StreetAddress.Trim(),
            City = input.City.Trim(),
            State = input.State.Trim(),
            PostalCode = input.PostalCode.Trim(),
        };
        if (vm.IsCompanyUser)
        {
            order.OrderStatus = SD.StatusApproved;
            order.PaymentStatus = SD.PaymentStatusDelayedPayment;
            order.PaymentDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(SD.CompanyPaymentTermDays));
        }
        else
        {
            order.OrderStatus = SD.StatusPending;
            order.PaymentStatus = SD.PaymentStatusPending;
        }

        unitOfWork.OrderHeader.Add(order);
        foreach (var c in vm.ShoppingCartList)
            unitOfWork.OrderDetail.Add(new OrderDetail { OrderHeaderId = order.Id, ProductId = c.ProductId, Count = c.Count, Price = c.Price });
        unitOfWork.Save();

        if (vm.IsCompanyUser)
        {
            unitOfWork.ShoppingCart.RemoveRange(vm.ShoppingCartList);
            unitOfWork.Save();
            return RedirectToAction(nameof(OrderConfirmation), new { id = order.Id });
        }

        try
        {
            var english = Culture.IsEnglish;
            var session = payments.CreateCheckout(order,
                vm.ShoppingCartList.Select(c => new CheckoutLine(c.Product.LocalTitle(english), c.Count, c.Price)),
                english,
                Url.Action(nameof(OrderConfirmation), "Cart", new { area = "Customer", id = order.Id }, Request.Scheme)!,
                Url.Action(nameof(Index), "Cart", new { area = "Customer" }, Request.Scheme)!,
                User.Identity?.Name);
            unitOfWork.OrderHeader.UpdateStripePaymentId(order.Id, session.Id, session.PaymentIntentId);
            unitOfWork.Save();
            // 303 tells the browser to follow the redirect with a GET.
            Response.Headers.Location = session.Url;
            return new StatusCodeResult(StatusCodes.Status303SeeOther);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe checkout could not be created for order {Order}", order.Id);
            unitOfWork.OrderHeader.UpdateStatus(order.Id, SD.StatusCancelled);
            unitOfWork.Save();
            TempData["error"] = t["pay.failed"].Value;
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> OrderConfirmation(Guid id)
    {
        var order = unitOfWork.OrderHeader.Get(o => o.Id == id);
        if (order == null || order.ApplicationUserId != User.UserId()) return NotFound();

        var paid = order.PaymentStatus is SD.PaymentStatusApproved or SD.PaymentStatusDelayedPayment;
        if (!paid)
        {
            try
            {
                paid = await payments.SyncAsync(order, OrderUrl(order.Id));
            }
            catch (StripeException ex)
            {
                logger.LogError(ex, "Stripe session check failed for order {Order}", order.Id);
            }
        }
        ViewBag.Paid = paid;
        return View(unitOfWork.OrderHeader.Get(o => o.Id == id));
    }

    [HttpPost]
    public IActionResult Plus(Guid cartId) => ChangeCount(cartId, +1);

    [HttpPost]
    public IActionResult Minus(Guid cartId) => ChangeCount(cartId, -1);

    [HttpPost]
    public IActionResult Remove(Guid cartId)
    {
        var cart = OwnCart(cartId);
        if (cart == null) return NotFound();
        unitOfWork.ShoppingCart.Remove(cart);
        unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    private IActionResult ChangeCount(Guid cartId, int delta)
    {
        var cart = OwnCart(cartId);
        if (cart == null) return NotFound();
        if (cart.Count + delta < 1)
            unitOfWork.ShoppingCart.Remove(cart);
        else
            cart.Count = PriceCalculator.ClampCount(cart.Count + delta);
        unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    // A cart row is reachable only by its owner: another user's id gives 404.
    private ShoppingCart? OwnCart(Guid cartId)
    {
        var userId = User.UserId();
        return unitOfWork.ShoppingCart.Get(c => c.Id == cartId && c.ApplicationUserId == userId, tracked: true);
    }

    private ShoppingCartVM BuildCart(string userId)
    {
        var list = unitOfWork.ShoppingCart.Query("Product")
            .Where(c => c.ApplicationUserId == userId)
            .OrderBy(c => c.Product.Title).ToList();
        foreach (var c in list)
            c.Price = PriceCalculator.UnitPrice(c.Count, c.Product.Price, c.Product.Price50, c.Product.Price100);

        var user = unitOfWork.ApplicationUser.Get(u => u.Id == userId);
        return new ShoppingCartVM
        {
            ShoppingCartList = list,
            IsCompanyUser = user?.CompanyId != null && User.IsInRole(SD.Role_Company),
            OrderHeader = new OrderHeader { OrderTotal = list.Sum(c => c.Price * c.Count) },
        };
    }

    private string OrderUrl(Guid id) =>
        Url.Action("Details", "Order", new { area = "Admin", id }, Request.Scheme)!;
}
