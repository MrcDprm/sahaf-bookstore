using System.Net;

namespace Sahaf.Web.Infrastructure;

public static class ClientIp
{
    // Render serves the site through Cloudflare. The right-most
    // X-Forwarded-For entry (what ForwardedHeaders gives us) is a Cloudflare
    // server, and a visitor's requests arrive through many of them, so a
    // per-IP limit keyed on it never fills up. Cloudflare puts the visitor's
    // address in CF-Connecting-IP and rejects requests that try to set it
    // themselves, so that header is used when present; locally the
    // connection address is.
    public const string CloudflareHeader = "CF-Connecting-IP";

    public static string Of(HttpContext context)
    {
        var header = context.Request.Headers[CloudflareHeader].ToString();
        if (IPAddress.TryParse(header, out var address)) return address.ToString();
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
