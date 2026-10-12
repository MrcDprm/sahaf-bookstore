using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Sahaf.Utility;
using Sahaf.Web.Infrastructure;
using Stripe;
using Stripe.Checkout;

namespace Sahaf.Web.Controllers;

// Stripe calls this endpoint when a checkout is paid or expires. Every
// request must carry a valid Stripe-Signature header; anything else is
// rejected before the body is used.
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class StripeWebhookController(
    PaymentService payments,
    IOptions<StripeSettings> settings,
    ILogger<StripeWebhookController> logger) : Controller
{
    [HttpPost("stripe/webhook")]
    [EnableRateLimiting("webhook")]
    [RequestSizeLimit(64 * 1024)]
    public async Task<IActionResult> Receive()
    {
        var secret = settings.Value.WebhookSecret;
        if (string.IsNullOrEmpty(secret)) return StatusCode(StatusCodes.Status503ServiceUnavailable);

        var signature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrEmpty(signature)) return BadRequest();

        var json = await new StreamReader(Request.Body).ReadToEndAsync();
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, secret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            logger.LogWarning("Rejected a Stripe webhook with an invalid signature");
            return BadRequest();
        }

        if (stripeEvent.Data.Object is not Session session ||
            !session.Metadata.TryGetValue("orderId", out var orderIdText) ||
            !Guid.TryParse(orderIdText, out var orderId))
            return Ok();

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
            case EventTypes.CheckoutSessionAsyncPaymentSucceeded:
                if (session.PaymentStatus == "paid")
                {
                    var orderUrl = Url.Action("Details", "Order", new { area = "Admin", id = orderId }, Request.Scheme)!;
                    await payments.MarkPaidAsync(orderId, session.Id, session.PaymentIntentId, orderUrl);
                }
                break;
            case EventTypes.CheckoutSessionExpired:
                payments.MarkExpired(orderId, session.Id);
                break;
        }
        return Ok();
    }
}
