using Microsoft.AspNetCore.HttpOverrides;

namespace Sahaf.Web.Infrastructure;

public static class ClientIp
{
    // In production Caddy runs on the same machine, ignores any X-Forwarded-For
    // the visitor sends and writes the address it sees. ForwardedHeaders believes
    // that header only from the default known proxy (127.0.0.1), so after it
    // runs the connection address is the visitor's and cannot be forged.
    // Headers such as CF-Connecting-IP are not read: nothing in front of the
    // site would overwrite them.
    public static string Of(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // Caddy terminates TLS and forwards plain HTTP with the visitor's address
    // and the original scheme. KnownProxies is left at its default (loopback).
    public static void ConfigureProxy(ForwardedHeadersOptions options) =>
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
}
