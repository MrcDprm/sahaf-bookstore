using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Models;
using Sahaf.Models.ViewModels;
using Sahaf.Utility;
using Sahaf.Web.Infrastructure;
using Sahaf.Web.Localization;

namespace Sahaf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class UserController(
    UserManager<ApplicationUser> userManager,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SharedResource> t,
    ILogger<UserController> logger) : Controller
{
    private const int PageSize = 20;

    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var query = unitOfWork.ApplicationUser.Query("Company");
        q = q?.Trim();
        if (!string.IsNullOrEmpty(q))
        {
            if (q.Length > 80) q = q[..80];
            var pattern = "%" + q.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
            query = query.Where(u => EF.Functions.ILike(u.Name, pattern) || EF.Functions.ILike(u.Email ?? "", pattern));
        }

        var total = query.Count();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);
        var users = query.OrderBy(u => u.Name).Skip((page - 1) * PageSize).Take(PageSize).ToList();

        var rows = new List<UserRow>();
        foreach (var u in users)
        {
            rows.Add(new UserRow
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email ?? "",
                PhoneNumber = u.PhoneNumber,
                CompanyName = u.Company?.Name,
                Role = (await userManager.GetRolesAsync(u)).FirstOrDefault() ?? "",
                IsLocked = u.LockoutEnd > DateTimeOffset.UtcNow,
                EmailConfirmed = u.EmailConfirmed,
            });
        }
        return View(new AdminListVM<UserRow> { Items = rows, Query = q, Page = page, TotalPages = totalPages, TotalCount = total });
    }

    [HttpPost]
    public async Task<IActionResult> LockUnlock(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (user.Id == User.UserId())
        {
            TempData["error"] = t["user.notSelf"].Value;
            return RedirectToAction(nameof(Index));
        }

        var locked = user.LockoutEnd > DateTimeOffset.UtcNow;
        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, locked ? null : DateTimeOffset.UtcNow.AddYears(100));
        // A new security stamp signs a locked user out of every device.
        await userManager.UpdateSecurityStampAsync(user);
        logger.LogInformation("User {User} {Action}", user.Id, locked ? "unlocked" : "locked");
        TempData["success"] = (locked ? t["user.unlocked"] : t["user.locked"]).Value;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> RoleManagement(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        return View(Fill(new RoleManagementVM
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email ?? "",
            Role = (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? SD.Role_Customer,
            CompanyId = user.CompanyId,
        }));
    }

    [HttpPost]
    public async Task<IActionResult> RoleManagement(RoleManagementVM vm)
    {
        var user = await userManager.FindByIdAsync(vm.UserId);
        if (user == null) return NotFound();
        vm.Name = user.Name;
        vm.Email = user.Email ?? "";

        if (user.Id == User.UserId())
            ModelState.AddModelError(nameof(vm.Role), t["user.notSelf"]);
        if (!SD.Roles.Contains(vm.Role))
            ModelState.AddModelError(nameof(vm.Role), t["account.invalidRole"]);
        if (vm.Role == SD.Role_Company &&
            (vm.CompanyId is not { } companyId || unitOfWork.Company.Get(c => c.Id == companyId) == null))
            ModelState.AddModelError(nameof(vm.CompanyId), t["account.companyRequired"]);
        if (!ModelState.IsValid) return View(Fill(vm));

        var current = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, current);
        await userManager.AddToRoleAsync(user, vm.Role);
        user.CompanyId = vm.Role == SD.Role_Company ? vm.CompanyId : null;
        await userManager.UpdateAsync(user);
        // Role claims live in the cookie; a new stamp makes the user sign in again.
        await userManager.UpdateSecurityStampAsync(user);
        logger.LogInformation("Role of user {User} changed to {Role}", user.Id, vm.Role);

        TempData["success"] = t["admin.updated"].Value;
        return RedirectToAction(nameof(Index));
    }

    private RoleManagementVM Fill(RoleManagementVM vm)
    {
        vm.RoleList = SD.Roles.Select(r => new SelectListItem(t["role." + r], r));
        vm.CompanyList = unitOfWork.Company.GetAll().OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()));
        return vm;
    }
}
