using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Sahaf.Models;
using Sahaf.Utility;
using Sahaf.Web.Localization;

namespace Sahaf.Web.Infrastructure;

public static class ClaimsExtensions
{
    public static string UserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The user is not signed in.");

    public static bool IsStaff(this ClaimsPrincipal user) =>
        user.IsInRole(SD.Role_Admin) || user.IsInRole(SD.Role_Employee);
}

public static class SecurityHeaders
{
    private const string Csp =
        "default-src 'self'; " +
        "img-src 'self' data:; " +
        "style-src 'self' https://fonts.googleapis.com; " +
        "font-src https://fonts.gstatic.com; " +
        "script-src 'self'; " +
        "connect-src 'self'; " +
        // After the checkout form posts, the browser is redirected to Stripe.
        "form-action 'self' https://checkout.stripe.com; " +
        "frame-ancestors 'none'; base-uri 'self'; object-src 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.ContentSecurityPolicy = Csp;
            headers.XContentTypeOptions = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers.XFrameOptions = "DENY";
            await next();
        });
}

// Model binding errors ("The value 'abc' is not valid") in the user's language.
public class LocalizedModelBindingMessages(IStringLocalizerFactory factory) : IConfigureOptions<MvcOptions>
{
    public void Configure(MvcOptions options)
    {
        var t = factory.Create(typeof(SharedResource));
        var m = options.ModelBindingMessageProvider;
        m.SetAttemptedValueIsInvalidAccessor((_, field) => t["val.invalid", field]);
        m.SetValueIsInvalidAccessor(_ => t["val.invalidValue"]);
        m.SetValueMustBeANumberAccessor(field => t["val.number", field]);
        m.SetValueMustNotBeNullAccessor(field => t["val.required", field]);
        m.SetMissingBindRequiredValueAccessor(field => t["val.required", field]);
        m.SetMissingKeyOrValueAccessor(() => t["val.invalidValue"]);
        m.SetMissingRequestBodyRequiredValueAccessor(() => t["val.invalidValue"]);
        m.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => t["val.invalidValue"]);
        m.SetNonPropertyUnknownValueIsInvalidAccessor(() => t["val.invalidValue"]);
        m.SetNonPropertyValueMustBeANumberAccessor(() => t["val.invalidValue"]);
        m.SetUnknownValueIsInvalidAccessor(field => t["val.invalid", "", field]);
    }
}

public class LocalizedIdentityErrorDescriber(IStringLocalizer<SharedResource> t) : IdentityErrorDescriber
{
    private IdentityError E(string code, params object[] args) =>
        new() { Code = code, Description = t["identity." + code, args] };

    public override IdentityError DefaultError() => E(nameof(DefaultError));
    public override IdentityError DuplicateEmail(string email) => E(nameof(DuplicateEmail), email);
    public override IdentityError DuplicateUserName(string userName) => E(nameof(DuplicateEmail), userName);
    public override IdentityError InvalidEmail(string? email) => E(nameof(InvalidEmail), email ?? "");
    public override IdentityError InvalidUserName(string? userName) => E(nameof(InvalidEmail), userName ?? "");
    public override IdentityError InvalidToken() => E(nameof(InvalidToken));
    public override IdentityError PasswordMismatch() => E(nameof(PasswordMismatch));
    public override IdentityError PasswordTooShort(int length) => E(nameof(PasswordTooShort), length);
    public override IdentityError PasswordRequiresDigit() => E(nameof(PasswordRequiresDigit));
    public override IdentityError PasswordRequiresLower() => E(nameof(PasswordRequiresLower));
    public override IdentityError PasswordRequiresUpper() => E(nameof(PasswordRequiresUpper));
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => E(nameof(PasswordRequiresUniqueChars), uniqueChars);
    public override IdentityError UserLockoutNotEnabled() => E(nameof(DefaultError));
}

// Sends the order confirmation e-mail. All values are HTML-encoded because
// the name and address were typed by the customer.
public class OrderEmails(IEmailSender sender, IStringLocalizer<SharedResource> t, ILogger<OrderEmails> logger)
{
    public async Task SendConfirmationAsync(OrderHeader order, string email, string orderUrl)
    {
        var html = HtmlEncoder.Default;
        var body =
            $"<p>{html.Encode(t["mail.order.hello", order.Name])}</p>" +
            $"<p>{html.Encode(t["mail.order.body", order.Number, Culture.Money(order.OrderTotal)])}</p>" +
            $"<p><a href=\"{html.Encode(orderUrl)}\">{html.Encode(t["mail.order.link"])}</a></p>" +
            $"<p>Sahaf</p>";
        try
        {
            await sender.SendEmailAsync(email, t["mail.order.subject", order.Number], body);
        }
        catch (Exception ex)
        {
            // A failed e-mail must not undo a paid order.
            logger.LogError(ex, "Order confirmation e-mail failed for order {Order}", order.Id);
        }
    }
}
