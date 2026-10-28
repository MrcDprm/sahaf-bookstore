using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Models;
using Sahaf.Utility;
using Sahaf.Web.Areas.Identity.Models;
using Sahaf.Web.Localization;

namespace Sahaf.Web.Areas.Identity.Controllers;

[Area("Identity")]
public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IUnitOfWork unitOfWork,
    IEmailSender emailSender,
    IStringLocalizer<SharedResource> t,
    ILogger<AccountController> logger) : Controller
{
    // ---------- Login / logout ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginVM { ReturnUrl = returnUrl });

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginVM model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
            return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl : "/");
        if (result.IsLockedOut)
            return Notice("account.lockedTitle", "account.lockedText");
        if (result.IsNotAllowed)
        {
            ViewBag.ShowResend = true;
            ModelState.AddModelError("", t["account.notConfirmed"]);
            return View(model);
        }
        // The same message for an unknown e-mail and a wrong password, so the
        // form cannot be used to find out who has an account.
        ModelState.AddModelError("", t["account.invalidLogin"]);
        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return LocalRedirect("/");
    }

    // ---------- Register ----------

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true && !User.IsInRole(SD.Role_Admin))
            return LocalRedirect("/");
        return View(FillLists(new RegisterVM()));
    }

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterVM model)
    {
        var byAdmin = User.IsInRole(SD.Role_Admin);
        if (User.Identity?.IsAuthenticated == true && !byAdmin) return LocalRedirect("/");

        var role = RegistrationRole(byAdmin, model.Role);
        int? companyId = null;
        if (role == null)
            ModelState.AddModelError(nameof(model.Role), t["account.invalidRole"]);
        else if (byAdmin)
        {
            if (role == SD.Role_Company)
            {
                if (model.CompanyId is not { } id || unitOfWork.Company.Get(c => c.Id == id) == null)
                    ModelState.AddModelError(nameof(model.CompanyId), t["account.companyRequired"]);
                companyId = model.CompanyId;
            }
        }
        if (!ModelState.IsValid) return View(FillLists(model));

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            Name = model.Name.Trim(),
            CompanyId = companyId,
        };
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors.DistinctBy(e => e.Description))
                ModelState.AddModelError(error.Code.StartsWith("Password") ? nameof(model.Password) : "", error.Description);
            return View(FillLists(model));
        }

        await userManager.AddToRoleAsync(user, role!);
        logger.LogInformation("New account created with role {Role}", role);
        await SendConfirmationAsync(user);

        if (byAdmin)
        {
            TempData["success"] = t["account.createdByAdmin"].Value;
            return RedirectToAction("Index", "User", new { area = "Admin" });
        }
        return Notice("account.checkMailTitle", "account.checkMailText");
    }

    /// <summary>
    /// Role is a posted field: only an admin may set it, and only to a known value.
    /// Everyone else becomes a plain customer, whatever they post. Null means an
    /// admin asked for an unknown role.
    /// </summary>
    public static string? RegistrationRole(bool byAdmin, string? requested)
    {
        if (!byAdmin || string.IsNullOrEmpty(requested)) return SD.Role_Customer;
        return SD.Roles.Contains(requested) ? requested : null;
    }

    // ---------- E-mail confirmation ----------

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? code)
    {
        if (userId == null || code == null) return LocalRedirect("/");
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return Notice("account.confirmFailTitle", "account.confirmFailText");

        var result = await userManager.ConfirmEmailAsync(user, Decode(code));
        return result.Succeeded
            ? Notice("account.confirmedTitle", "account.confirmedText", "nav.login", "/Identity/Account/Login")
            : Notice("account.confirmFailTitle", "account.confirmFailText", "account.resend", "/Identity/Account/ResendConfirmation");
    }

    [HttpGet]
    public IActionResult ResendConfirmation() => View(new EmailVM());

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendConfirmation(EmailVM model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user != null && !user.EmailConfirmed) await SendConfirmationAsync(user);
        // Same answer whether or not the address exists.
        return Notice("account.checkMailTitle", "account.resendSentText");
    }

    // ---------- Password reset ----------

    [HttpGet]
    public IActionResult ForgotPassword() => View(new EmailVM());

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(EmailVM model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user != null && user.EmailConfirmed)
        {
            var code = Encode(await userManager.GeneratePasswordResetTokenAsync(user));
            var link = Url.Action(nameof(ResetPassword), "Account", new { area = "Identity", code }, Request.Scheme)!;
            await SendAsync(user.Email!, t["mail.reset.subject"], t["mail.reset.body"], t["mail.reset.button"], link);
        }
        return Notice("account.checkMailTitle", "account.resetSentText");
    }

    [HttpGet]
    public IActionResult ResetPassword(string? code) =>
        code == null ? LocalRedirect("/") : View(new ResetPasswordVM { Code = code });

    [HttpPost]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            var result = await userManager.ResetPasswordAsync(user, Decode(model.Code), model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors.DistinctBy(e => e.Description))
                    ModelState.AddModelError(error.Code.StartsWith("Password") ? nameof(model.Password) : "", error.Description);
                return View(model);
            }
        }
        return Notice("account.resetDoneTitle", "account.resetDoneText", "nav.login", "/Identity/Account/Login");
    }

    // ---------- Profile ----------

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Manage()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        return View(new ProfileVM
        {
            Email = user.Email ?? "",
            Name = user.Name,
            PhoneNumber = user.PhoneNumber,
            StreetAddress = user.StreetAddress,
            City = user.City,
            State = user.State,
            PostalCode = user.PostalCode,
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Manage(ProfileVM model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        model.Email = user.Email ?? "";
        if (!ModelState.IsValid) return View(model);

        user.Name = model.Name.Trim();
        user.PhoneNumber = model.PhoneNumber?.Trim();
        user.StreetAddress = model.StreetAddress?.Trim();
        user.City = model.City?.Trim();
        user.State = model.State?.Trim();
        user.PostalCode = model.PostalCode?.Trim();
        await userManager.UpdateAsync(user);

        TempData["success"] = t["account.profileSaved"].Value;
        return RedirectToAction(nameof(Manage));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordVM());

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors.DistinctBy(e => e.Description))
                ModelState.AddModelError(error.Code == "PasswordMismatch" ? nameof(model.OldPassword) : nameof(model.NewPassword), error.Description);
            return View(model);
        }
        // New security stamp: other devices are signed out, this one stays in.
        await signInManager.RefreshSignInAsync(user);
        TempData["success"] = t["account.passwordChanged"].Value;
        return RedirectToAction(nameof(Manage));
    }

    [HttpGet]
    [Authorize]
    public IActionResult Delete() => View(new DeleteAccountVM());

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Delete(DeleteAccountVM model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (await userManager.IsInRoleAsync(user, SD.Role_Admin))
        {
            ModelState.AddModelError("", t["account.adminCannotDelete"]);
            return View(model);
        }
        if (!await userManager.CheckPasswordAsync(user, model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), t["identity.PasswordMismatch"]);
            return View(model);
        }

        await userManager.DeleteAsync(user);
        await signInManager.SignOutAsync();
        logger.LogInformation("An account was deleted by its owner");
        return Notice("account.deletedTitle", "account.deletedText", "error.home", "/");
    }

    // ---------- Notices ----------

    [HttpGet]
    public IActionResult AccessDenied() => Notice("error.403.title", "account.accessDeniedText", "error.home", "/");

    private ViewResult Notice(string title, string text, string? linkText = null, string? linkUrl = null) =>
        View("Notice", new NoticeVM(t[title], t[text], linkText == null ? null : t[linkText].Value, linkUrl));

    // ---------- Helpers ----------

    private RegisterVM FillLists(RegisterVM model)
    {
        if (!User.IsInRole(SD.Role_Admin)) return model;
        model.RoleList = SD.Roles.Select(r => new SelectListItem(t["role." + r], r));
        model.CompanyList = unitOfWork.Company.GetAll().OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()));
        return model;
    }

    private async Task SendConfirmationAsync(ApplicationUser user)
    {
        var code = Encode(await userManager.GenerateEmailConfirmationTokenAsync(user));
        var link = Url.Action(nameof(ConfirmEmail), "Account", new { area = "Identity", userId = user.Id, code }, Request.Scheme)!;
        await SendAsync(user.Email!, t["mail.confirm.subject"], t["mail.confirm.body", user.Name], t["mail.confirm.button"], link);
    }

    private async Task SendAsync(string to, string subject, string text, string button, string link)
    {
        var html = HtmlEncoder.Default;
        var body =
            $"<p>{html.Encode(text)}</p>" +
            $"<p><a href=\"{html.Encode(link)}\">{html.Encode(button)}</a></p>" +
            $"<p style=\"color:#777\">{html.Encode(t["mail.ignore"])}</p><p>Sahaf</p>";
        try
        {
            await emailSender.SendEmailAsync(to, subject, body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Account e-mail could not be sent");
        }
    }

    // Tokens contain "+" and "/", so they travel Base64Url-encoded in links.
    private static string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string Decode(string code)
    {
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code)); }
        catch (FormatException) { return ""; }
    }
}
