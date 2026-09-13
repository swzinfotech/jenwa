using Microsoft.AspNetCore.Http;

namespace Jenwa.Inquiry.Services;

/// <summary>
/// The browser-facing CORS headers come from the Function App's platform CORS setting, which
/// provision.ps1 configures: the Functions host answers the preflight OPTIONS itself and never
/// invokes the function, so headers written here would never reach a preflight, and writing them
/// on the real response as well duplicates what the platform already adds.
///
/// What is left here is the server-side origin check, which refuses a request whose Origin is not
/// on the allow list instead of relying on the browser to discard the response.
/// </summary>
public static class Cors
{
    /// <summary>
    /// Returns the request's origin when it is acceptable, or null when the request must be refused.
    /// A missing Origin header is allowed — browsers always send one for a cross-origin POST, so the
    /// header-less case is a server-side or curl call, which CORS does not protect anyway.
    /// </summary>
    public static string? Resolve(HttpRequest request, AppConfig config)
    {
        var origin = request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin)) return "";
        if (config.AllowedOrigins.Count == 0 || config.AllowedOrigins.Contains("*")) return origin;
        return config.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase) ? origin : null;
    }

    /// <summary>
    /// The visitor's address as seen by the platform. Azure puts the real client in
    /// X-Forwarded-For as "ip:port", possibly followed by upstream proxies.
    /// </summary>
    public static string ClientIp(HttpRequest request)
    {
        var forwarded = request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrEmpty(forwarded))
        {
            var first = forwarded.Split(',')[0].Trim();
            if (first.StartsWith('['))
            {
                var close = first.IndexOf(']');
                if (close > 0) return first[1..close];
            }
            // Only strip a port when there is exactly one colon, so bare IPv6 survives.
            var colon = first.LastIndexOf(':');
            if (colon > 0 && first.IndexOf(':') == colon) return first[..colon];
            if (first.Length > 0) return first;
        }
        return request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
