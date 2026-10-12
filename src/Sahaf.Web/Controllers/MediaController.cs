using Microsoft.AspNetCore.Mvc;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Utility;

namespace Sahaf.Web.Controllers;

public class MediaController(IUnitOfWork unitOfWork) : Controller
{
    // Generated cover. The language is in the URL so caches keep one copy per language.
    [HttpGet("cover/{id:int}.svg")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public IActionResult Cover(int id, string? l)
    {
        var product = unitOfWork.Product.Get(p => p.Id == id);
        if (product == null) return NotFound();
        var english = l == "en";
        var svg = CoverGenerator.Svg(product.Id, product.LocalTitle(english), product.Author,
            english ? "Sahaf Classics" : "Sahaf Klasikleri");
        return Content(svg, "image/svg+xml; charset=utf-8");
    }

    // Uploaded image. The content type was detected from the file bytes at
    // upload time, and nosniff stops the browser from guessing another one.
    [HttpGet("img/{id:guid}")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
    public IActionResult Image(Guid id)
    {
        var image = unitOfWork.ProductImage.Get(i => i.Id == id);
        if (image == null) return NotFound();
        return File(image.Data, image.ContentType);
    }

    [HttpGet("lang/{code}")]
    public IActionResult Language(string code, string? returnUrl)
    {
        var culture = code == "en" ? "en" : "tr";
        Response.Cookies.Append(
            Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
            Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(new(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                HttpOnly = true,
            });
        // Only local paths: an open redirect would let others abuse this link.
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
