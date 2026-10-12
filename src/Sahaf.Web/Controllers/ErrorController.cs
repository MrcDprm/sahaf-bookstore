using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Sahaf.Web.Controllers;

// Friendly error pages. Details stay in the log; the user never sees a
// stack trace.
[AllowAnonymous]
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
