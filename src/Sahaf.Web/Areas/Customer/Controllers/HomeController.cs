using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Models;
using Sahaf.Models.ViewModels;
using Sahaf.Utility;
using Sahaf.Web.Infrastructure;
using Sahaf.Web.Localization;

namespace Sahaf.Web.Areas.Customer.Controllers;

[Area("Customer")]
public class HomeController(IUnitOfWork unitOfWork, IStringLocalizer<SharedResource> t) : Controller
{
    public const int PageSize = 12;

    public IActionResult Index()
    {
        var products = unitOfWork.Product.Query("Category");
        var vm = new HomeVM
        {
            Shelf = products.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.Id % 7).ThenBy(p => p.Id).Take(16).ToList(),
            Featured = products.Where(p => p.IsFeatured).OrderBy(p => p.Title).Take(8).ToList(),
            Newest = products.OrderByDescending(p => p.CreatedAt).Take(8).ToList(),
            Deals = products.Where(p => p.ListPrice > p.Price)
                .OrderByDescending(p => (p.ListPrice - p.Price) / p.ListPrice).Take(4).ToList(),
            Categories = CategoryCounts(),
            BookCount = products.Count(),
            AuthorCount = products.Select(p => p.Author).Distinct().Count(),
        };
        return View(vm);
    }

    public IActionResult Catalog(string? q, int? categoryId, string sort = "new", int page = 1)
    {
        var query = unitOfWork.Product.Query("Category");

        q = q?.Trim();
        if (!string.IsNullOrEmpty(q))
        {
            if (q.Length > 80) q = q[..80];
            // Escape the LIKE wildcards so "%" in the box is searched literally.
            var pattern = "%" + q.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
            query = query.Where(p => EF.Functions.ILike(p.Title, pattern)
                                     || EF.Functions.ILike(p.TitleEn ?? "", pattern)
                                     || EF.Functions.ILike(p.Author, pattern));
        }
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId);

        query = sort switch
        {
            "price-asc" => query.OrderBy(p => p.Price).ThenBy(p => p.Id),
            "price-desc" => query.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            "title" => query.OrderBy(p => p.Title).ThenBy(p => p.Id),
            "discount" => query.OrderByDescending(p => (p.ListPrice - p.Price) / p.ListPrice).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
        };

        var total = query.Count();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        return View(new CatalogVM
        {
            Products = query.Skip((page - 1) * PageSize).Take(PageSize).ToList(),
            Categories = CategoryCounts(),
            Query = q,
            CategoryId = categoryId,
            Sort = sort,
            Page = page,
            TotalPages = totalPages,
            TotalCount = total,
        });
    }

    public IActionResult Details(int id)
    {
        var product = unitOfWork.Product.Get(p => p.Id == id, "Category");
        if (product == null) return NotFound();

        // Only the ids: the image bytes are served by MediaController.
        var imageIds = unitOfWork.ProductImage.Query()
            .Where(i => i.ProductId == id).OrderBy(i => i.SortOrder).Select(i => i.Id).ToList();

        var related = unitOfWork.Product.Query("Category")
            .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
            .OrderBy(p => p.Id % 5).ThenBy(p => p.Id)
            .Take(4).ToList();
        return View(new DetailsVM { Product = product, ImageIds = imageIds, Related = related });
    }

    [HttpPost]
    [Authorize]
    public IActionResult AddToCart(int productId, int count)
    {
        if (unitOfWork.Product.Get(p => p.Id == productId) == null) return NotFound();
        if (count < 1 || count > PriceCalculator.MaxCount)
        {
            TempData["error"] = t["cart.countRange", PriceCalculator.MaxCount].Value;
            return RedirectToAction(nameof(Details), new { id = productId });
        }

        var userId = User.UserId();
        var cart = unitOfWork.ShoppingCart.Get(c => c.ApplicationUserId == userId && c.ProductId == productId, tracked: true);
        if (cart == null)
        {
            unitOfWork.ShoppingCart.Add(new ShoppingCart { ApplicationUserId = userId, ProductId = productId, Count = count });
        }
        else
        {
            cart.Count = PriceCalculator.ClampCount(cart.Count + count);
        }
        unitOfWork.Save();

        TempData["success"] = t["cart.added"].Value;
        return RedirectToAction(nameof(Details), new { id = productId });
    }

    public IActionResult About() => View();

    private List<CategoryCount> CategoryCounts()
    {
        var counts = unitOfWork.Product.Query()
            .GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionary(x => x.Key, x => x.Count);
        return unitOfWork.Category.Query().OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList()
            .Select(c => new CategoryCount(c, counts.GetValueOrDefault(c.Id)))
            .ToList();
    }
}
