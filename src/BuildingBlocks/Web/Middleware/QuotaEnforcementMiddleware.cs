using Dental.Framework.Quota;
using Dental.Framework.Shared.Http;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Dental.Framework.Web.Middleware;

/// <summary>
/// Charges one <see cref="QuotaResource.ApiCalls"/> unit per request and refuses the request with
/// 429 once the tenant's allowance is spent.
/// </summary>
/// <remarks>
/// Runs AFTER authentication (it needs a resolved tenant) and after the rate limiter, so a caller
/// hitting a burst limit does not also burn quota. Health and metrics paths, and requests with no
/// resolved tenant, are skipped.
/// </remarks>
/// <param name="next">Next middleware.</param>
/// <param name="logger">Logger.</param>
public sealed class QuotaEnforcementMiddleware(
    RequestDelegate next,
    ILogger<QuotaEnforcementMiddleware> logger)
{
    /// <summary>Runs the middleware.</summary>
    /// <param name="context">The request.</param>
    /// <param name="quotaService">Quota service, resolved per request.</param>
    /// <param name="tenantContextAccessor">Supplies the resolved tenant.</param>
    /// <returns>A task that completes when the pipeline has run.</returns>
    public async Task InvokeAsync(
        HttpContext context,
        IQuotaService quotaService,
        IMultiTenantContextAccessor tenantContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(quotaService);
        ArgumentNullException.ThrowIfNull(tenantContextAccessor);

        string? tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;

        if (string.IsNullOrEmpty(tenantId) || IsUnmetered(context.Request.Path))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        QuotaUsage usage = await quotaService
            .CheckAndRecordAsync(tenantId, QuotaResource.ApiCalls, 1, context.RequestAborted)
            .ConfigureAwait(false);

        if (usage.IsExceeded)
        {
            logger.LogWarning(
                "Tenant {TenantId} exceeded its {Resource} quota of {Limit}.",
                tenantId,
                usage.Resource,
                usage.Limit);

            await WriteQuotaExceededAsync(context, usage).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool IsUnmetered(PathString path) =>
        ApiRoutes.Unmetered.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

    private static async Task WriteQuotaExceededAsync(HttpContext context, QuotaUsage usage)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (usage.WindowEndsAt is { } windowEnd)
        {
            int retryAfter = (int)Math.Max(1, (windowEnd - DateTimeOffset.UtcNow).TotalSeconds);
            context.Response.Headers[HeaderNames.RetryAfter] =
                retryAfter.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        ProblemDetails problem = new()
        {
            Title = "Quota exceeded.",
            Detail = $"The {usage.Resource} quota of {usage.Limit} has been reached for this tenant.",
            Status = StatusCodes.Status429TooManyRequests,
        };

        problem.Extensions["resource"] = usage.Resource.ToString();
        problem.Extensions["limit"] = usage.Limit;
        problem.Extensions["windowEndsAt"] = usage.WindowEndsAt;

        await context.Response
            .WriteAsJsonAsync(problem, options: null, "application/problem+json", context.RequestAborted)
            .ConfigureAwait(false);
    }
}
