using Microsoft.AspNetCore.Http;

namespace Dental.Framework.Web.Middleware;

/// <summary>
/// Adds the standard hardening headers.
/// </summary>
/// <remarks>
/// <c>/scalar</c> and <c>/openapi</c> are excluded from the CSP: the API reference UI loads and
/// executes its own scripts, and a strict policy blanks the page with no visible error.
/// </remarks>
/// <param name="next">Next middleware.</param>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "img-src 'self' data: blob:; " +
        "style-src 'self' 'unsafe-inline'; " +
        "script-src 'self'; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    /// <summary>Runs the middleware.</summary>
    /// <param name="context">The request.</param>
    /// <returns>A task that completes when the pipeline has run.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        IHeaderDictionary headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        if (!IsApiDocumentation(context.Request.Path))
        {
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
        }

        return next(context);
    }

    private static bool IsApiDocumentation(PathString path) =>
        path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase);
}
