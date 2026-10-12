using Microsoft.Extensions.Options;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Models;
using Sahaf.Utility;
using Stripe;
using Stripe.Checkout;

namespace Sahaf.Web.Infrastructure;

public record CheckoutLine(string Name, int Count, decimal UnitPrice);

// Everything that talks to Stripe. Both the success redirect and the webhook
// call MarkPaidAsync, so it must be safe to run twice for the same order.
public class PaymentService(
    IUnitOfWork unitOfWork,
    IOptions<StripeSettings> settings,
    OrderEmails emails,
    ILogger<PaymentService> logger)
{
    public bool IsConfigured => settings.Value.IsConfigured;

    public Session CreateCheckout(OrderHeader order, IEnumerable<CheckoutLine> lines, bool english,
        string successUrl, string cancelUrl, string? customerEmail)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = order.Id.ToString(),
            CustomerEmail = customerEmail,
            Locale = english ? "en" : "tr",
            // Unpaid sessions expire (Stripe allows 30 minutes at the least);
            // the webhook then cancels the pending order.
            ExpiresAt = DateTime.UtcNow.AddMinutes(35),
            Metadata = new Dictionary<string, string> { ["orderId"] = order.Id.ToString() },
            LineItems = lines.Select(l => new SessionLineItemOptions
            {
                Quantity = l.Count,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "try",
                    UnitAmount = PriceCalculator.ToMinorUnits(l.UnitPrice),
                    ProductData = new SessionLineItemPriceDataProductDataOptions { Name = l.Name },
                },
            }).ToList(),
        };
        return new SessionService().Create(options);
    }

    // Asks Stripe whether the order's checkout session has been paid.
    public async Task<bool> SyncAsync(OrderHeader order, string orderUrl)
    {
        if (order.PaymentStatus == SD.PaymentStatusApproved) return true;
        if (string.IsNullOrEmpty(order.SessionId) || !IsConfigured) return false;

        var session = await new SessionService().GetAsync(order.SessionId);
        if (session.PaymentStatus != "paid") return false;

        await MarkPaidAsync(order.Id, session.Id, session.PaymentIntentId, orderUrl);
        return true;
    }

    public async Task MarkPaidAsync(Guid orderId, string sessionId, string? paymentIntentId, string orderUrl)
    {
        var order = unitOfWork.OrderHeader.Get(o => o.Id == orderId, "ApplicationUser");
        // The session must be the one this order created: a paid session can
        // never mark a different order as paid.
        if (order == null || order.PaymentStatus == SD.PaymentStatusApproved || order.SessionId != sessionId) return;

        // A company order paid later keeps its current order status.
        var wasDelayed = order.PaymentStatus == SD.PaymentStatusDelayedPayment;
        unitOfWork.OrderHeader.UpdateStripePaymentId(order.Id, sessionId, paymentIntentId);
        unitOfWork.OrderHeader.UpdateStatus(order.Id,
            wasDelayed ? order.OrderStatus : SD.StatusApproved, SD.PaymentStatusApproved);

        if (!wasDelayed)
        {
            var cart = unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == order.ApplicationUserId);
            unitOfWork.ShoppingCart.RemoveRange(cart);
        }
        unitOfWork.Save();
        logger.LogInformation("Order {Order} marked as paid", order.Id);

        if (!wasDelayed && order.ApplicationUser.Email is { } email)
            await emails.SendConfirmationAsync(order, email, orderUrl);
    }

    // An abandoned checkout: the order was never paid, so it is cancelled.
    public void MarkExpired(Guid orderId, string sessionId)
    {
        var order = unitOfWork.OrderHeader.Get(o => o.Id == orderId);
        if (order == null || order.SessionId != sessionId || order.PaymentStatus != SD.PaymentStatusPending) return;
        unitOfWork.OrderHeader.UpdateStatus(order.Id, SD.StatusCancelled);
        unitOfWork.Save();
    }

    public void Refund(OrderHeader order)
    {
        new RefundService().Create(new RefundCreateOptions
        {
            PaymentIntent = order.PaymentIntentId,
            Reason = RefundReasons.RequestedByCustomer,
        });
    }
}
