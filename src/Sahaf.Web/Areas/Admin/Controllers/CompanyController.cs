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
public class CompanyController(IUnitOfWork unitOfWork, IStringLocalizer<SharedResource> t) : Controller
{
    public IActionResult Index()
    {
        ViewBag.Counts = unitOfWork.ApplicationUser.Query().Where(u => u.CompanyId != null)
            .GroupBy(u => u.CompanyId!.Value).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionary(x => x.Key, x => x.Count);
        return View(unitOfWork.Company.Query().OrderBy(c => c.Name).ToList());
    }

    public IActionResult Upsert(int? id)
    {
        if (id is null or 0) return View(new Company());
        var company = unitOfWork.Company.Get(c => c.Id == id);
        return company == null ? NotFound() : View(company);
    }

    [HttpPost]
    public IActionResult Upsert(Company input)
    {
        if (!ModelState.IsValid) return View(input);

        var entity = new Company();
        entity.Name = input.Name.Trim();
        entity.StreetAddress = input.StreetAddress?.Trim();
        entity.City = input.City?.Trim();
        entity.State = input.State?.Trim();
        entity.PostalCode = input.PostalCode?.Trim();
        entity.PhoneNumber = input.PhoneNumber?.Trim();

        if (input.Id == 0)
        {
            unitOfWork.Company.Add(entity);
            TempData["success"] = t["admin.created"].Value;
        }
        else
        {
            if (unitOfWork.Company.Get(c => c.Id == input.Id) == null) return NotFound();
            entity.Id = input.Id;
            unitOfWork.Company.Update(entity);
            TempData["success"] = t["admin.updated"].Value;
        }
        unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var company = unitOfWork.Company.Get(c => c.Id == id, tracked: true);
        if (company == null) return NotFound();
        if (unitOfWork.ApplicationUser.Query().Any(u => u.CompanyId == id))
        {
            TempData["error"] = t["admin.companyInUse"].Value;
            return RedirectToAction(nameof(Index));
        }
        unitOfWork.Company.Remove(company);
        unitOfWork.Save();
        TempData["success"] = t["admin.deleted"].Value;
        return RedirectToAction(nameof(Index));
    }
}
