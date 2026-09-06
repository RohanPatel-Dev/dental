using Dental.Framework.Shared.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Dental.Framework.Web.Middleware;

/// <summary>
/// Replays the cached response for a repeated <c>Idempotency-Key</c> on endpoints that opted in
/// with <c>WithIdempotency()</c>.
/// </summary>
/// <param name="next">Next middleware.</param>
/// <param name="cache">Cache holding recorded responses.</param>
/// <param name="logger">Logger.</param>
public sealed class IdempotencyMiddleware(
    RequestDelegate next,
    HybridCache cache,
    ILogger<IdempotencyMiddleware> logger)
{
    private const int MaxKeyLength = 128;
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    /// <summary>Runs the middleware.</summary>
    /// <param name="context">The request.</param>
    /// <returns>A task that completes when the pipeline has run.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.GetEndpoint()?.Metadata.GetMetadata<IdempotencyMetadata>() is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        string? key = context.Request.Headers[HeaderNames.IdempotencyKey];
        if (string.IsNullOrWhiteSpace(key))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (key.Length > MaxKeyLength)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response
                .WriteAsJsonAsync(
                    new { title = $"Idempotency-Key must be at most {MaxKeyLength} characters." },
                    context.RequestAborted)
                .ConfigureAwait(false);
            return;
        }

        // Scoped by tenant: two tenants may legitimately reuse the same client generated key.
        string tenantId = context.Request.Headers[Shared.Tenancy.TenantConstants.Header].ToString();
        string cacheKey = $"idempotency:{tenantId}:{key}";

        RecordedResponse? recorded = await cache
            .GetOrCreateAsync<RecordedResponse?>(
                cacheKey,
                _ => ValueTask.FromResult<RecordedResponse?>(null),
                new HybridCacheEntryOptions { Expiration = Ttl, Flags = HybridCacheEntryFlags.DisableUnderlyingData },
                cancellationToken: context.RequestAborted)
            .ConfigureAwait(false);

        if (recorded is not null)
        {
            logger.LogInformation("Replaying idempotent response for key {IdempotencyKey}.", key);

            context.Response.StatusCode = recorded.StatusCode;
            context.Response.ContentType = recorded.ContentType;
            context.Response.Headers[HeaderNames.IdempotencyReplayed] = "true";
            await context.Response.WriteAsync(recorded.Body, context.RequestAborted).ConfigureAwait(false);
            return;
        }

        Stream original = context.Response.Body;
        using MemoryStream buffer = new();
        context.Response.Body = buffer;

        try
        {
            await next(context).ConfigureAwait(false);

            buffer.Position = 0;
            string body;
            using (StreamReader reader = new(buffer, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);
            }

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                await cache.SetAsync(
                        cacheKey,
                        new RecordedResponse(
                            context.Response.StatusCode,
                            context.Response.ContentType ?? "application/json",
                            body),
                        new HybridCacheEntryOptions { Expiration = Ttl },
                        cancellationToken: context.RequestAborted)
                    .ConfigureAwait(false);
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(original, context.RequestAborted).ConfigureAwait(false);
        }
        finally
        {
            context.Response.Body = original;
        }
    }

    private sealed record RecordedResponse(int StatusCode, string ContentType, string Body);

}

/// <summary>Marks an endpoint as replay safe. Added by <c>WithIdempotency()</c>.</summary>
public sealed class IdempotencyMetadata
{
    /// <summary>How long a recorded response stays replayable.</summary>
    public TimeSpan Ttl { get; init; } = TimeSpan.FromHours(24);
}
