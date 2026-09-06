using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Dental.Framework.Shared.Http;
using Dental.Framework.Shared.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace Dental.Framework.Web.RateLimiting;

/// <summary>Chained partitioned fixed windows: tenant, then user, then IP.</summary>
public static class RateLimitingRegistration
{
    /// <summary>Name of the strict policy applied to authentication endpoints.</summary>
    public const string AuthPolicy = "auth";

    /// <summary>Registers the global limiter and the strict auth policy.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddHeroRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(nameof(RateLimitingOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        RateLimitingOptions options =
            configuration.GetSection(nameof(RateLimitingOptions)).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        if (!options.Enabled)
        {
            return services;
        }

        TimeSpan window = TimeSpan.FromSeconds(options.WindowSeconds);

        services.AddRateLimiter(limiter =>
        {
            limiter.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                BuildPartition(
                    context => context.Request.Headers[TenantConstants.Header].ToString(),
                    "tenant",
                    options.TenantPermitLimit,
                    window),
                BuildPartition(
                    context => context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                    "user",
                    options.UserPermitLimit,
                    window),
                BuildPartition(
                    context => context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                    "ip",
                    options.IpPermitLimit,
                    window));

            limiter.AddFixedWindowLimiter(AuthPolicy, fixedWindow =>
            {
                fixedWindow.PermitLimit = options.AuthPermitLimit;
                fixedWindow.Window = window;
                fixedWindow.QueueLimit = 0;
            });

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers[HeaderNames.RetryAfter] =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                ProblemDetails problem = new()
                {
                    Title = "Too many requests.",
                    Detail = "The request was rejected by the rate limiter. Retry after a short wait.",
                    Status = StatusCodes.Status429TooManyRequests,
                };

                await context.HttpContext.Response
                    .WriteAsJsonAsync(problem, options: null, "application/problem+json", cancellationToken)
                    .ConfigureAwait(false);
            };
        });

        return services;
    }

    private static PartitionedRateLimiter<HttpContext> BuildPartition(
        Func<HttpContext, string> keySelector,
        string prefix,
        int permitLimit,
        TimeSpan window) =>
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            if (IsUnmetered(context.Request.Path))
            {
                return RateLimitPartition.GetNoLimiter("unmetered");
            }

            string key = keySelector(context);
            if (string.IsNullOrEmpty(key))
            {
                return RateLimitPartition.GetNoLimiter($"{prefix}:anonymous");
            }

            return RateLimitPartition.GetFixedWindowLimiter(
                $"{prefix}:{key}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                });
        });

    private static bool IsUnmetered(PathString path) =>
        ApiRoutes.Unmetered.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
}
