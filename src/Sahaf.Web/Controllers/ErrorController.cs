using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sahaf.Web.Controllers;

// Friendly error pages. Details stay in the log; the user never sees a
// stack trace. Errors are re-executed with the original method, so a failed
// POST arrives here as a POST: the antiforgery check must not turn it into a 400.
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorController : Controller
{
    [Route("Error/{code:int?}")]
    public IActionResult Index(int? code)
    {
        ViewBag.Code = code ?? 500;
        return View("~/Views/Shared/Error.cshtml");
    }
}
