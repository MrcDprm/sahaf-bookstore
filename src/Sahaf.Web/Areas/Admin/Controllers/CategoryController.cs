using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Models;
using Sahaf.Utility;
using Sahaf.Web.Localization;

namespace Sahaf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class CategoryController(IUnitOfWork unitOfWork, IStringLocalizer<SharedResource> t) : Controller
{
    public IActionResult Index()
    {
        var counts = unitOfWork.Product.Query().GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionary(x => x.Key, x => x.Count);
        ViewBag.Counts = counts;
        return View(unitOfWork.Category.Query().OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList());
    }

    public IActionResult Upsert(int? id)
    {
        if (id is null or 0) return View(new Category { DisplayOrder = NextOrder() });
        var category = unitOfWork.Category.Get(c => c.Id == id);
        return category == null ? NotFound() : View(category);
    }

    [HttpPost]
    public IActionResult Upsert(Category input)
    {
        input.Name = input.Name?.Trim() ?? "";
        input.NameEn = string.IsNullOrWhiteSpace(input.NameEn) ? null : input.NameEn.Trim();
        if (unitOfWork.Category.Get(c => c.Name.ToLower() == input.Name.ToLower() && c.Id != input.Id) != null)
            ModelState.AddModelError(nameof(input.Name), t["admin.duplicateName"]);
        if (!ModelState.IsValid) return View(input);

        if (input.Id == 0)
        {
            unitOfWork.Category.Add(new Category { Name = input.Name, NameEn = input.NameEn, DisplayOrder = input.DisplayOrder });
            TempData["success"] = t["admin.created"].Value;
        }
        else
        {
            if (unitOfWork.Category.Get(c => c.Id == input.Id) == null) return NotFound();
            unitOfWork.Category.Update(input);
            TempData["success"] = t["admin.updated"].Value;
        }
        unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var category = unitOfWork.Category.Get(c => c.Id == id, tracked: true);
        if (category == null) return NotFound();
        // Books keep their category (restricted foreign key): move them first.
        if (unitOfWork.Product.Query().Any(p => p.CategoryId == id))
        {
            TempData["error"] = t["admin.categoryInUse"].Value;
            return RedirectToAction(nameof(Index));
        }
        unitOfWork.Category.Remove(category);
        unitOfWork.Save();
        TempData["success"] = t["admin.deleted"].Value;
        return RedirectToAction(nameof(Index));
    }

    private int NextOrder() =>
        Math.Min(100, (unitOfWork.Category.Query().Max(c => (int?)c.DisplayOrder) ?? 0) + 1);
}
