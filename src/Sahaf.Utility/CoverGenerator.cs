using System.Globalization;
using System.Security;
using System.Text;

namespace Sahaf.Utility;

// Draws a typographic book cover as SVG. Every book in the Sahaf series gets
// one: the same id always gives the same palette and pattern, so no image
// files are needed and there are no cover copyrights to worry about.
public static class CoverGenerator
{
    public const int Width = 400;
    public const int Height = 600;

    private record Palette(string Background, string Accent, string Ink);

    private static readonly Palette[] Palettes =
    [
        new("#1f3a5f", "#e8b04b", "#f6efe2"),
        new("#7a2e1f", "#f0c987", "#fbf3e4"),
        new("#2f4a3a", "#d9a441", "#f3eddc"),
        new("#efe4cf", "#b5442e", "#1f1b16"),
        new("#20252b", "#c9a46a", "#efe7d6"),
        new("#5b3a5e", "#f2b6a0", "#fbf0ea"),
        new("#c75b39", "#1f2a36", "#fdf4e6"),
        new("#e9d8a6", "#255f5a", "#1c1a17"),
        new("#0f4c5c", "#f3a712", "#f7f1e3"),
        new("#3d2c23", "#d97b4a", "#f4ead9"),
        new("#d6e2d3", "#3b5b3f", "#1b211b"),
        new("#8a1c2b", "#f4d58d", "#fff6e8"),
    ];

    public static string PaletteBackground(int id) => Palettes[Index(id, Palettes.Length)].Background;
    public static string PaletteAccent(int id) => Palettes[Index(id, Palettes.Length)].Accent;
    public static string PaletteInk(int id) => Palettes[Index(id, Palettes.Length)].Ink;

    public static string Svg(int id, string title, string author, string series)
    {
        var p = Palettes[Index(id, Palettes.Length)];
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Width} {Height}\" width=\"{Width}\" height=\"{Height}\">");
        sb.Append($"<rect width=\"{Width}\" height=\"{Height}\" fill=\"{p.Background}\"/>");
        sb.Append(Pattern(Index(id * 7 + 3, 6), p.Accent));

        // Spine shadow on the left edge makes the flat cover read as a book.
        sb.Append("<rect width=\"14\" height=\"600\" fill=\"#000\" opacity=\".18\"/>");
        sb.Append("<rect x=\"14\" width=\"2\" height=\"600\" fill=\"#fff\" opacity=\".12\"/>");

        const string serif = "font-family=\"Fraunces, Georgia, &apos;Times New Roman&apos;, serif\"";
        sb.Append(CultureInfo.InvariantCulture,
            $"<text x=\"200\" y=\"58\" text-anchor=\"middle\" {serif} font-size=\"13\" letter-spacing=\"4\" fill=\"{p.Ink}\" opacity=\".85\">{Escape(series.ToUpperInvariant())}</text>");
        sb.Append(CultureInfo.InvariantCulture,
            $"<text x=\"200\" y=\"80\" text-anchor=\"middle\" {serif} font-size=\"12\" fill=\"{p.Ink}\" opacity=\".7\">No. {id:000}</text>");

        // Title block on a solid panel so it stays readable over any pattern.
        var lines = Wrap(title, 13, 4);
        var fontSize = lines.Max(l => l.Length) > 11 || lines.Count > 3 ? 34 : 42;
        var lineHeight = fontSize * 1.15;
        var blockHeight = lines.Count * lineHeight + 44;
        var top = 300 - blockHeight / 2;
        sb.Append(CultureInfo.InvariantCulture,
            $"<rect x=\"44\" y=\"{top:0}\" width=\"312\" height=\"{blockHeight:0}\" fill=\"{p.Background}\" opacity=\".92\"/>");
        sb.Append(CultureInfo.InvariantCulture,
            $"<rect x=\"52\" y=\"{top + 8:0}\" width=\"296\" height=\"{blockHeight - 16:0}\" fill=\"none\" stroke=\"{p.Accent}\" stroke-width=\"1.5\"/>");
        for (var i = 0; i < lines.Count; i++)
        {
            var y = top + 22 + fontSize * 0.95 + i * lineHeight;
            sb.Append(CultureInfo.InvariantCulture,
                $"<text x=\"200\" y=\"{y:0}\" text-anchor=\"middle\" {serif} font-size=\"{fontSize}\" font-weight=\"600\" fill=\"{p.Ink}\">{Escape(lines[i])}</text>");
        }

        sb.Append(CultureInfo.InvariantCulture,
            $"<line x1=\"170\" y1=\"508\" x2=\"230\" y2=\"508\" stroke=\"{p.Accent}\" stroke-width=\"2\"/>");
        sb.Append(CultureInfo.InvariantCulture,
            $"<text x=\"200\" y=\"540\" text-anchor=\"middle\" {serif} font-size=\"18\" font-style=\"italic\" fill=\"{p.Ink}\">{Escape(Truncate(author, 30))}</text>");
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string Pattern(int kind, string color) => kind switch
    {
        // Horizontal bands
        0 => $"<g fill=\"{color}\" opacity=\".55\"><rect y=\"110\" width=\"400\" height=\"10\"/><rect y=\"132\" width=\"400\" height=\"4\"/><rect y=\"464\" width=\"400\" height=\"4\"/><rect y=\"480\" width=\"400\" height=\"10\"/></g>",
        // Rising sun
        1 => $"<g fill=\"{color}\"><circle cx=\"200\" cy=\"300\" r=\"190\" opacity=\".35\"/><circle cx=\"200\" cy=\"300\" r=\"130\" opacity=\".35\"/></g>",
        // Dot grid
        2 => DotGrid(color),
        // Diagonal lines
        3 => $"<g stroke=\"{color}\" stroke-width=\"3\" opacity=\".4\">" +
             string.Concat(Enumerable.Range(-12, 26).Select(i => $"<line x1=\"{i * 40}\" y1=\"600\" x2=\"{i * 40 + 600}\" y2=\"0\"/>")) + "</g>",
        // Concentric arcs from the bottom corner
        4 => $"<g fill=\"none\" stroke=\"{color}\" stroke-width=\"8\" opacity=\".45\">" +
             string.Concat(Enumerable.Range(1, 9).Select(i => $"<circle cx=\"400\" cy=\"600\" r=\"{i * 60}\"/>")) + "</g>",
        // Ornamental frame
        _ => $"<g fill=\"none\" stroke=\"{color}\" opacity=\".7\"><rect x=\"24\" y=\"24\" width=\"352\" height=\"552\" stroke-width=\"3\"/><rect x=\"34\" y=\"34\" width=\"332\" height=\"532\" stroke-width=\"1\"/></g>" +
             $"<g fill=\"{color}\" opacity=\".7\"><path d=\"M200 96 l10 10 -10 10 -10 -10z\"/><path d=\"M200 470 l10 10 -10 10 -10 -10z\"/></g>",
    };

    private static string DotGrid(string color)
    {
        var sb = new StringBuilder($"<g fill=\"{color}\" opacity=\".45\">");
        for (var y = 30; y < Height; y += 36)
            for (var x = 30 + (y / 36 % 2) * 18; x < Width; x += 36)
                sb.Append($"<circle cx=\"{x}\" cy=\"{y}\" r=\"4\"/>");
        return sb.Append("</g>").ToString();
    }

    // Greedy word wrap. A word longer than the line is kept whole; the font
    // size is reduced instead.
    public static List<string> Wrap(string text, int maxChars, int maxLines)
    {
        var lines = new List<string>();
        var current = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.Length == 0) current = word;
            else if (current.Length + 1 + word.Length <= maxChars) current += " " + word;
            else { lines.Add(current); current = word; }
        }
        if (current.Length > 0) lines.Add(current);
        if (lines.Count == 0) lines.Add("");
        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToList();
            lines[^1] = Truncate(lines[^1] + "…", maxChars);
        }
        return lines;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    private static string Escape(string s) => SecurityElement.Escape(s) ?? "";

    private static int Index(int seed, int length) => (int)((uint)(seed * 2654435761u) % (uint)length);
}
