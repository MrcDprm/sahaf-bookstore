using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Sahaf.DataAccess.Data;
using Sahaf.DataAccess.DbInitializer;
using Sahaf.DataAccess.Repository;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Models;
using Sahaf.Utility;
using Sahaf.Web.Infrastructure;
using Sahaf.Web.Localization;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Render/Neon give DATABASE_URL as a URI; locally appsettings.Development.json is used.
var connectionString = builder.Configuration["DATABASE_URL"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No database connection string configured.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(ConnectionStrings.Normalize(connectionString)));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<LocalizedIdentityErrorDescriber>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});
// Antiforgery and TempData cookies get the Secure flag on HTTPS, like the
// sign-in cookie (their default is to never set it).
builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);
builder.Services.Configure<CookieTempDataProviderOptions>(options =>
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);

// The cookie's security stamp is checked on every request (default: every
// 30 minutes), so locking a user or changing a role takes effect at once.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.Zero);
builder.Services.AddDataProtection()
    .SetApplicationName("Sahaf")
    .PersistKeysToDbContext<ApplicationDbContext>();

// Localization: my JSON localizer must be registered before AddLocalization.
builder.Services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
builder.Services.AddLocalization();
builder.Services.AddSingleton<IConfigureOptions<MvcOptions>, LocalizedModelBindingMessages>();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = new[] { new CultureInfo("tr-TR"), new CultureInfo("en-GB") };
    options.DefaultRequestCulture = new RequestCulture("tr-TR");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.RequestCultureProviders = [new CookieRequestCultureProvider(), new AcceptLanguageHeaderRequestCultureProvider()];
});

// Razor encodes everything outside Basic Latin by default ("ı" becomes &#x131;).
// Allowing all ranges keeps Turkish text readable; <, >, & and quotes are still encoded.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddControllersWithViews(options =>
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    options.AddPolicy("webhook", context => RateLimitPartition.GetFixedWindowLimiter(
        "stripe", _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Render terminates TLS at its proxy and forwards plain HTTP.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Behind Render's proxy the public port is always 443.
builder.Services.AddHttpsRedirection(options => options.HttpsPort = 443);

builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddHttpClient<IEmailSender, ResendEmailSender>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDbInitializer, DbInitializer>();
builder.Services.AddScoped<OrderEmails>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

StripeConfiguration.ApiKey = app.Services.GetRequiredService<IOptions<StripeSettings>>().Value.SecretKey;

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    // Render's health check calls the container over plain HTTP; a redirect
    // there would mark the service as down.
    app.UseWhen(context => !context.Request.Path.StartsWithSegments("/healthz"),
        branch => branch.UseHttpsRedirection());
}
app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseSecurityHeaders();
app.UseStaticFiles(new StaticFileOptions
{
    // Files linked with asp-append-version (?v=hash) change URL when their
    // content changes, so browsers may keep them for a year.
    OnPrepareResponse = context =>
    {
        if (context.Context.Request.Query.ContainsKey("v"))
            context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
    },
});
app.UseRequestLocalization();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<IDbInitializer>().InitializeAsync();

app.Run();

// Lets the test project reach Program.
public partial class Program;
