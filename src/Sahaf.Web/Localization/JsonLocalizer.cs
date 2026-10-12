using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Localization;

namespace Sahaf.Web.Localization;

// Marker type for IStringLocalizer<SharedResource>.
public class SharedResource;

// All UI text lives in two flat JSON files (Resources/tr.json, en.json)
// instead of .resx files. The same localizer also translates the
// "val.*" / "field.*" keys used in the model attributes.
public class JsonStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly JsonStringLocalizer _localizer;

    public JsonStringLocalizerFactory(IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "Resources");
        var tables = new Dictionary<string, Dictionary<string, string>>();
        foreach (var lang in Culture.Supported)
        {
            var json = File.ReadAllText(Path.Combine(dir, $"{lang}.json"));
            tables[lang] = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
        }
        _localizer = new JsonStringLocalizer(tables);
    }

    public IStringLocalizer Create(Type resourceSource) => _localizer;
    public IStringLocalizer Create(string baseName, string location) => _localizer;
}

public class JsonStringLocalizer(Dictionary<string, Dictionary<string, string>> tables) : IStringLocalizer
{
    public LocalizedString this[string name]
    {
        get
        {
            var found = TryGet(name, out var value);
            return new LocalizedString(name, value, resourceNotFound: !found);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var found = TryGet(name, out var value);
            return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, value, arguments), !found);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        Table().Select(kv => new LocalizedString(kv.Key, kv.Value));

    private Dictionary<string, string> Table() =>
        tables.GetValueOrDefault(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) ?? tables[Culture.Default];

    private bool TryGet(string name, out string value)
    {
        if (Table().TryGetValue(name, out value!) || tables[Culture.Default].TryGetValue(name, out value!))
            return true;
        value = name;
        return false;
    }
}

public static class Culture
{
    public const string Default = "tr";
    public static readonly string[] Supported = ["tr", "en"];

    public static bool IsEnglish => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-GB");

    // Prices are always in Turkish lira; only the number format follows the language.
    public static string Money(decimal value) =>
        IsEnglish ? "₺" + value.ToString("N2", En) : value.ToString("N2", Tr) + " ₺";

    public static string Date(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Istanbul)
            .ToString("d MMM yyyy HH:mm", IsEnglish ? En : Tr);

    public static string Date(DateOnly date) => date.ToString("d MMM yyyy", IsEnglish ? En : Tr);

    public static string Year(int year) =>
        year < 0 ? (IsEnglish ? $"{-year} BC" : $"MÖ {-year}") : year.ToString(CultureInfo.InvariantCulture);

    private static readonly TimeZoneInfo Istanbul = FindIstanbul();

    private static TimeZoneInfo FindIstanbul()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("TRT", TimeSpan.FromHours(3), "TRT", "TRT"); }
    }
}
