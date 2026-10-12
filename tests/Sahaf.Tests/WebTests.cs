using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sahaf.DataAccess.Data;
using Sahaf.DataAccess.Repository;
using Sahaf.Models;
using Sahaf.Utility;
using Sahaf.Web.Areas.Customer.Controllers;
using Sahaf.Web.Infrastructure;
using Sahaf.Web.Localization;

namespace Sahaf.Tests;

public class ResourceTests
{
    private static Dictionary<string, string> Load(string lang)
    {
        var dir = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(dir, "src"))) dir = Path.GetDirectoryName(dir)!;
        var path = Path.Combine(dir, "src", "Sahaf.Web", "Resources", $"{lang}.json");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))!;
    }

    [Fact]
    public void Turkish_and_English_have_the_same_keys()
    {
        var tr = Load("tr");
        var en = Load("en");
        Assert.Empty(tr.Keys.Except(en.Keys));
        Assert.Empty(en.Keys.Except(tr.Keys));
    }

    [Fact]
    public void Every_text_is_filled_and_uses_the_same_placeholders_in_both_languages()
    {
        var tr = Load("tr");
        var en = Load("en");
        var placeholder = new Regex(@"\{\d\}");
        foreach (var (key, value) in tr)
        {
            Assert.False(string.IsNullOrWhiteSpace(value), key);
            Assert.False(string.IsNullOrWhiteSpace(en[key]), key);
            var a = placeholder.Matches(value).Select(m => m.Value).Distinct().Order();
            var b = placeholder.Matches(en[key]).Select(m => m.Value).Distinct().Order();
            Assert.True(a.SequenceEqual(b), $"{key}: {value} / {en[key]}");
        }
    }
}

public class ViewTests
{
    // Regression: the form tag helper adds the CSRF token only to forms without
    // an explicit action; one admin form posted without it and always failed.
    [Fact]
    public void Every_post_form_with_an_action_carries_an_antiforgery_token()
    {
        var dir = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(dir, "src"))) dir = Path.GetDirectoryName(dir)!;
        var views = Directory.GetFiles(Path.Combine(dir, "src", "Sahaf.Web"), "*.cshtml", SearchOption.AllDirectories);

        var missing = new List<string>();
        foreach (var file in views)
        {
            var html = File.ReadAllText(file);
            foreach (Match form in Regex.Matches(html, "<form[^>]*method=\"post\"[^>]*action=[^>]*>"))
            {
                var end = html.IndexOf("</form>", form.Index, StringComparison.Ordinal);
                if (!html[form.Index..end].Contains("AntiForgeryToken"))
                    missing.Add($"{Path.GetFileName(file)}: {form.Value}");
            }
        }
        Assert.True(views.Length > 20);
        Assert.Empty(missing);
    }
}

// The cart must only ever touch the signed-in user's own rows.
public class CartOwnershipTests
{
    private sealed class KeyLocalizer : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, name);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    private sealed class NoMail : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage) => Task.CompletedTask;
    }

    private static (CartController Controller, ApplicationDbContext Db) Create(string userId)
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var unitOfWork = new UnitOfWork(db);
        var localizer = new KeyLocalizer();
        var payments = new PaymentService(unitOfWork, Options.Create(new StripeSettings()),
            new OrderEmails(new NoMail(), localizer, NullLogger<OrderEmails>.Instance), NullLogger<PaymentService>.Instance);
        var controller = new CartController(unitOfWork, payments, localizer, NullLogger<CartController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test")),
                },
            },
        };
        return (controller, db);
    }

    private static ShoppingCart Seed(ApplicationDbContext db, string owner, int count)
    {
        var category = new Category { Name = "Raf", DisplayOrder = 1 };
        var product = new Product { Title = "Kitap", Author = "Yazar", Description = "D", Price = 10, ListPrice = 10, Price50 = 9, Price100 = 8, Category = category };
        var cart = new ShoppingCart { ApplicationUserId = owner, Product = product, Count = count };
        db.ShoppingCarts.Add(cart);
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return cart;
    }

    [Fact]
    public void Another_users_cart_row_cannot_be_removed_or_changed()
    {
        var (controller, db) = Create("attacker");
        var cart = Seed(db, "owner", 3);

        Assert.IsType<NotFoundResult>(controller.Remove(cart.Id));
        Assert.IsType<NotFoundResult>(controller.Plus(cart.Id));
        Assert.IsType<NotFoundResult>(controller.Minus(cart.Id));
        Assert.Equal(3, db.ShoppingCarts.Single().Count);
    }

    [Fact]
    public void The_owner_can_change_and_remove_their_row()
    {
        var (controller, db) = Create("owner");
        var cart = Seed(db, "owner", 1);

        Assert.IsType<RedirectToActionResult>(controller.Plus(cart.Id));
        Assert.Equal(2, db.ShoppingCarts.AsNoTracking().Single().Count);

        controller.Minus(cart.Id);
        controller.Minus(cart.Id);
        Assert.Empty(db.ShoppingCarts);
    }
}
