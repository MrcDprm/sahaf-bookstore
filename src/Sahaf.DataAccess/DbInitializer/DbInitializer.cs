using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sahaf.DataAccess.Data;
using Sahaf.Models;
using Sahaf.Utility;

namespace Sahaf.DataAccess.DbInitializer;

public interface IDbInitializer
{
    Task InitializeAsync();
}

// Runs once at startup: applies migrations, creates the roles, the admin
// account (only from configuration, never a hard-coded password) and the
// starter catalog.
public class DbInitializer(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration,
    ILogger<DbInitializer> logger) : IDbInitializer
{
    public async Task InitializeAsync()
    {
        await db.Database.MigrateAsync();

        foreach (var role in SD.Roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        await CreateAdminAsync();
        await SeedCatalogAsync();
    }

    private async Task CreateAdminAsync()
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin:Email / Admin:Password are not set; no admin account was created.");
            return;
        }
        if (await userManager.FindByEmailAsync(email) != null) return;

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Name = "Sahaf Admin",
        };
        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            logger.LogError("Admin account could not be created: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Code)));
            return;
        }
        await userManager.AddToRoleAsync(admin, SD.Role_Admin);
    }

    private async Task SeedCatalogAsync()
    {
        if (await db.Categories.AnyAsync()) return;

        var categories = SeedData.Categories
            .Select((c, i) => new Category { Name = c.Tr, NameEn = c.En, DisplayOrder = i + 1 })
            .ToList();
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        db.Products.AddRange(SeedData.Products(categories));
        db.Companies.Add(new Company
        {
            Name = "Mavi Kalem Okulları",
            StreetAddress = "Moda Caddesi 12",
            City = "İstanbul",
            State = "Kadıköy",
            PostalCode = "34710",
            PhoneNumber = "02160000000",
        });
        await db.SaveChangesAsync();
    }
}
