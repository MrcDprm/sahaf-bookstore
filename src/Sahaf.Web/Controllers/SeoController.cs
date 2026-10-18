using Microsoft.AspNetCore.Mvc;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Web.Infrastructure;

namespace Sahaf.Web.Controllers;

public class SeoController(IUnitOfWork unitOfWork) : Controller
{
    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Sitemap()
    {
        var paths = new List<string> { "/", "/Customer/Home/Catalog", "/Customer/Home/About" };
        paths.AddRange(unitOfWork.Category.Query().OrderBy(c => c.DisplayOrder).Select(c => $"/Customer/Home/Catalog?categoryId={c.Id}"));
        paths.AddRange(unitOfWork.Product.Query().OrderBy(p => p.Id).Select(p => $"/Customer/Home/Details/{p.Id}"));
        return Content(Seo.Sitemap(paths), "application/xml; charset=utf-8");
    }

    [HttpGet("robots.txt")]
    [ResponseCache(Duration = 86400)]
    public IActionResult Robots() => Content(Seo.Robots, "text/plain; charset=utf-8");
}
