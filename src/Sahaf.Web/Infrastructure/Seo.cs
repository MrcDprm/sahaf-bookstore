using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Sahaf.Web.Infrastructure;

// Search engines: one address per page and language, structured data, sitemap.
public static class Seo
{
    public const string SiteUrl = "https://sahaf.miracdeprem.com";
    public const string DefaultImage = "/og-image.png";

    // Only parameters that change the content stay in the canonical address;
    // sorting and searching do not create new pages.
    private static readonly string[] KeptQuery = ["categoryId", "page"];

    private static readonly string[] HomePaths = ["/", "/Customer", "/Customer/Home", "/Customer/Home/Index"];

    public static string CanonicalPath(string path)
    {
        var trimmed = path.Length > 1 ? path.TrimEnd('/') : path;
        return HomePaths.Contains(trimmed, StringComparer.OrdinalIgnoreCase) ? "/" : trimmed;
    }

    /// <summary>Absolute address of the page in one language (English adds lang=en).</summary>
    public static string PageUrl(string path, IQueryCollection query, bool english)
    {
        var parts = new List<string>();
        foreach (var key in KeptQuery)
        {
            var value = query[key].ToString();
            if (value.Length == 0 || (key == "page" && value == "1")) continue;
            parts.Add($"{key}={Uri.EscapeDataString(value)}");
        }
        if (english) parts.Add("lang=en");
        return SiteUrl + CanonicalPath(path) + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
    }

    /// <summary>The current address without the lang parameter (for the language switch).</summary>
    public static string WithoutLang(string path, IQueryCollection query)
    {
        var parts = query.Where(q => !q.Key.Equals("lang", StringComparison.OrdinalIgnoreCase))
            .SelectMany(q => q.Value.Select(v => $"{Uri.EscapeDataString(q.Key)}={Uri.EscapeDataString(v ?? "")}"))
            .ToList();
        return path + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
    }

    public static string Absolute(string pathOrUrl) =>
        pathOrUrl.StartsWith("http", StringComparison.Ordinal) ? pathOrUrl : SiteUrl + pathOrUrl;

    /// <summary>Shortens text for a meta description at a word boundary.</summary>
    public static string Summary(string text, int max = 155)
    {
        var flat = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (flat.Length <= max) return flat;
        var cut = flat.LastIndexOf(' ', max - 1);
        return flat[..(cut > 0 ? cut : max - 1)].TrimEnd(',', '.', ';', ':') + "…";
    }

    /// <summary>JSON-LD that cannot close its script tag.</summary>
    public static string JsonLd(object data) =>
        JsonSerializer.Serialize(data).Replace("<", "\\u003c", StringComparison.Ordinal);

    /// <summary>Sitemap with the Turkish address and both language alternates for each page.</summary>
    public static string Sitemap(IEnumerable<string> paths)
    {
        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\" xmlns:xhtml=\"http://www.w3.org/1999/xhtml\">");
        foreach (var path in paths)
        {
            var tr = WebUtility.HtmlEncode(SiteUrl + path);
            var en = WebUtility.HtmlEncode(SiteUrl + path + (path.Contains('?') ? "&" : "?") + "lang=en");
            foreach (var loc in new[] { tr, en })
            {
                xml.AppendLine("  <url>");
                xml.AppendLine($"    <loc>{loc}</loc>");
                xml.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"tr\" href=\"{tr}\"/>");
                xml.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"en\" href=\"{en}\"/>");
                xml.AppendLine($"    <xhtml:link rel=\"alternate\" hreflang=\"x-default\" href=\"{tr}\"/>");
                xml.AppendLine("  </url>");
            }
        }
        xml.AppendLine("</urlset>");
        return xml.ToString();
    }

    public const string Robots =
        "User-agent: *\n" +
        "Disallow: /Admin/\n" +
        "Disallow: /Identity/\n" +
        "Disallow: /Customer/Cart/\n" +
        "Disallow: /lang/\n" +
        "\n" +
        "Sitemap: " + SiteUrl + "/sitemap.xml\n";
}
