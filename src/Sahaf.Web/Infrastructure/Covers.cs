using Sahaf.Models;
using Sahaf.Utility;
using Sahaf.Web.Localization;

namespace Sahaf.Web.Infrastructure;

public static class Covers
{
    // Uploaded cover if there is one, otherwise the generated series cover.
    public static string Url(Product p) =>
        p.CoverImageId is { } imageId
            ? $"/img/{imageId}"
            : $"/cover/{p.Id}.svg?l={(Culture.IsEnglish ? "en" : "tr")}";

    public static string Background(Product p) => CoverGenerator.PaletteBackground(p.Id);
    public static string Accent(Product p) => CoverGenerator.PaletteAccent(p.Id);
    public static string Ink(Product p) => CoverGenerator.PaletteInk(p.Id);
}
