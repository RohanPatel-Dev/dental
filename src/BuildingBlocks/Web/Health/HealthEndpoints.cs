using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dental.Framework.Web.Health;

/// <summary>Liveness and readiness endpoints.</summary>
public static class HealthEndpoints
{
    /// <summary>Tag marking a check as a readiness (rather than liveness) probe.</summary>
    public const string ReadyTag = "ready";

    /// <summary>Maps <c>/alive</c>, <c>/ready</c> and <c>/health</c>.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapHeroHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Liveness must not touch a dependency: a failing database should not make the orchestrator
        // restart a perfectly healthy process.
        endpoints.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = _ => false,
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag),
            ResponseWriter = WriteResponseAsync,
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteResponseAsync,
        }).AllowAnonymous();

        return endpoints;
    }

    private static async Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds,
                    description = e.Value.Description,
                },
                StringComparer.Ordinal),
        };

        await context.Response
            .WriteAsync(JsonSerializer.Serialize(payload, JsonSerializerOptions.Web), context.RequestAborted)
            .ConfigureAwait(false);
    }
}
