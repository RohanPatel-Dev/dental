using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Dental.Framework.Jobs;

/// <summary>Basic auth gate for the Hangfire dashboard.</summary>
/// <param name="options">Job options carrying the dashboard credentials.</param>
public sealed class JobDashboardAuthorizationFilter(IOptions<JobOptions> options) : IDashboardAuthorizationFilter
{
    private readonly JobOptions _options = options.Value;

    /// <inheritdoc />
    public bool Authorize(DashboardContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        HttpContext httpContext = context.GetHttpContext();
        string? header = httpContext.Request.Headers.Authorization;

        if (string.IsNullOrWhiteSpace(header)
            || !AuthenticationHeaderValue.TryParse(header, out AuthenticationHeaderValue? parsed)
            || !string.Equals(parsed.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(parsed.Parameter))
        {
            return Challenge(httpContext);
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parsed.Parameter));
        }
        catch (FormatException)
        {
            return Challenge(httpContext);
        }

        int separator = decoded.IndexOf(':', StringComparison.Ordinal);
        if (separator < 0)
        {
            return Challenge(httpContext);
        }

        string user = decoded[..separator];
        string password = decoded[(separator + 1)..];

        bool ok = FixedTimeEquals(user, _options.DashboardUser)
                  && FixedTimeEquals(password, _options.DashboardPassword);

        return ok || Challenge(httpContext);
    }

    private static bool Challenge(HttpContext httpContext)
    {
        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire\"";
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return false;
    }

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left),
            Encoding.UTF8.GetBytes(right));
}
