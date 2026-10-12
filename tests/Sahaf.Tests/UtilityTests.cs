using System.Xml.Linq;
using Sahaf.Utility;

namespace Sahaf.Tests;

public class CoverGeneratorTests
{
    [Fact]
    public void Same_book_always_gets_the_same_cover() =>
        Assert.Equal(CoverGenerator.Svg(12, "Sefiller", "Victor Hugo", "Sahaf"),
                     CoverGenerator.Svg(12, "Sefiller", "Victor Hugo", "Sahaf"));

    [Fact]
    public void Text_is_escaped_and_the_svg_stays_valid_xml()
    {
        var svg = CoverGenerator.Svg(1, "<script>alert('x')</script> & co", "\"Yazar\" <b>", "Seri");

        Assert.DoesNotContain("<script>", svg);
        Assert.DoesNotContain("<b>", svg);
        var doc = XDocument.Parse(svg);
        Assert.Contains("<script>", doc.Root!.Value);
    }

    [Fact]
    public void Evenly_spaced_ids_do_not_all_share_one_palette()
    {
        // Regression: books 4, 16, 28 and 40 (the deals shelf) used to get the same colour.
        var colours = new[] { 4, 16, 28, 40, 52 }.Select(CoverGenerator.PaletteBackground).Distinct().Count();
        Assert.True(colours > 2);
    }

    [Fact]
    public void Long_titles_are_wrapped_and_cut_with_an_ellipsis()
    {
        var lines = CoverGenerator.Wrap("bir iki üç dört beş altı yedi sekiz dokuz on on bir on iki on üç", 10, 3);

        Assert.Equal(3, lines.Count);
        Assert.All(lines, l => Assert.True(l.Length <= 10, l));
        Assert.EndsWith("…", lines[^1]);
    }

    [Fact]
    public void Empty_title_gives_one_empty_line() =>
        Assert.Equal([""], CoverGenerator.Wrap("   ", 10, 3));
}

public class ImageValidatorTests
{
    [Fact]
    public void Detects_jpeg_png_and_webp_by_their_first_bytes()
    {
        Assert.Equal("image/jpeg", ImageValidator.DetectContentType([0xFF, 0xD8, 0xFF, 0xE0]));
        Assert.Equal("image/png", ImageValidator.DetectContentType([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0]));
        Assert.Equal("image/webp", ImageValidator.DetectContentType("RIFF\0\0\0\0WEBPVP8 "u8));
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<svg onload=alert(1)>")]
    [InlineData("GIF89a")]
    [InlineData("")]
    public void Rejects_anything_else(string content) =>
        Assert.Null(ImageValidator.DetectContentType(System.Text.Encoding.UTF8.GetBytes(content)));

    [Fact]
    public void Rejects_a_riff_file_that_is_not_webp() =>
        Assert.Null(ImageValidator.DetectContentType("RIFF\0\0\0\0WAVEfmt "u8));
}

public class ConnectionStringsTests
{
    [Fact]
    public void Converts_a_neon_uri_to_npgsql_format()
    {
        var result = ConnectionStrings.Normalize("postgresql://sahaf:p%40ss%3Bword@ep-cool.eu-central-1.aws.neon.tech/sahafdb?sslmode=require");

        Assert.Contains("Host=ep-cool.eu-central-1.aws.neon.tech", result);
        Assert.Contains("Port=5432", result);
        Assert.Contains("Database=sahafdb", result);
        Assert.Contains("Username=sahaf", result);
        Assert.Contains("Password=p@ss;word", result);
        Assert.Contains("SSL Mode=Require", result);
    }

    [Fact]
    public void Defaults_to_ssl_and_keeps_an_explicit_port()
    {
        var result = ConnectionStrings.Normalize("postgres://u:p@db.example.com:6543/app");

        Assert.Contains("Port=6543", result);
        Assert.Contains("SSL Mode=Require", result);
    }

    [Fact]
    public void Leaves_key_value_strings_unchanged()
    {
        const string value = "Host=localhost;Database=sahaf;Username=postgres";
        Assert.Equal(value, ConnectionStrings.Normalize(value));
    }
}
