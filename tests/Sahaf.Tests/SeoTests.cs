using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Sahaf.Web.Infrastructure;

namespace Sahaf.Tests;

public class SeoTests
{
    private static QueryCollection Query(params (string Key, string Value)[] items) =>
        new(items.ToDictionary(i => i.Key, i => new StringValues(i.Value)));

    [Theory]
    [InlineData("/", "/")]
    [InlineData("/Customer/Home/Index", "/")]
    [InlineData("/customer/home", "/")]
    [InlineData("/Customer/Home/Details/5/", "/Customer/Home/Details/5")]
    public void Home_has_one_canonical_address(string path, string expected) =>
        Assert.Equal(expected, Seo.CanonicalPath(path));

    [Fact]
    public void Canonical_keeps_category_and_page_but_not_sort_search_or_first_page()
    {
        var query = Query(("categoryId", "3"), ("sort", "price-asc"), ("q", "tolstoy"), ("page", "2"));
        Assert.Equal("https://sahaf.miracdeprem.com/Customer/Home/Catalog?categoryId=3&page=2",
            Seo.PageUrl("/Customer/Home/Catalog", query, english: false));
        Assert.Equal("https://sahaf.miracdeprem.com/Customer/Home/Catalog?lang=en",
            Seo.PageUrl("/Customer/Home/Catalog", Query(("page", "1")), english: true));
    }

    // Regression: the language switch sent the user back to "?lang=en",
    // so on an English address the switch to Turkish did nothing.
    [Fact]
    public void Language_switch_returns_without_the_lang_parameter()
    {
        var query = Query(("lang", "en"), ("categoryId", "3"));
        Assert.Equal("/Customer/Home/Catalog?categoryId=3", Seo.WithoutLang("/Customer/Home/Catalog", query));
    }

    [Fact]
    public void Summary_cuts_at_a_word_and_flattens_whitespace()
    {
        var text = string.Join(' ', Enumerable.Repeat("kelime", 60)) + "\n\nson";
        var summary = Seo.Summary(text);
        Assert.True(summary.Length <= 156);
        Assert.EndsWith("kelime…", summary);
        Assert.Equal("Kısa metin", Seo.Summary("Kısa   metin"));
    }

    [Fact]
    public void Json_ld_cannot_close_the_script_tag()
    {
        var json = Seo.JsonLd(new { name = "</script><script>alert(1)</script>" });
        Assert.DoesNotContain("</script>", json);
    }

    [Fact]
    public void Sitemap_lists_both_languages_with_alternates_and_escapes_ampersands()
    {
        var xml = Seo.Sitemap(["/", "/Customer/Home/Catalog?categoryId=3"]);
        var doc = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var locs = doc.Descendants(ns + "loc").Select(l => l.Value).ToList();
        Assert.Equal(
        [
            "https://sahaf.miracdeprem.com/",
            "https://sahaf.miracdeprem.com/?lang=en",
            "https://sahaf.miracdeprem.com/Customer/Home/Catalog?categoryId=3",
            "https://sahaf.miracdeprem.com/Customer/Home/Catalog?categoryId=3&lang=en",
        ], locs);
        Assert.Equal(12, doc.Descendants(XNamespace.Get("http://www.w3.org/1999/xhtml") + "link").Count());
    }

    [Fact]
    public void Robots_hides_private_areas_and_points_to_the_sitemap()
    {
        Assert.Contains("Disallow: /Admin/", Seo.Robots);
        Assert.Contains("Disallow: /Customer/Cart/", Seo.Robots);
        Assert.Contains("Sitemap: https://sahaf.miracdeprem.com/sitemap.xml", Seo.Robots);
    }
}
