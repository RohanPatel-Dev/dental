namespace Dental.Framework.Shared.Http;

/// <summary>Custom header names shared by the API and both SPAs.</summary>
public static class HeaderNames
{
    /// <summary>Correlation identifier accepted from, and echoed back to, the caller.</summary>
    public const string CorrelationId = "X-Correlation-ID";

    /// <summary>Client supplied key that makes a POST replay safe.</summary>
    public const string IdempotencyKey = "Idempotency-Key";

    /// <summary>Set on a response served from the idempotency cache.</summary>
    public const string IdempotencyReplayed = "Idempotency-Replayed";

    /// <summary>Seconds until a rate limited caller may retry.</summary>
    public const string RetryAfter = "Retry-After";

    /// <summary>Disables proxy buffering so server sent events stream promptly.</summary>
    public const string AccelBuffering = "X-Accel-Buffering";
}

/// <summary>Route fragments that must agree between the API and the SPAs.</summary>
public static class ApiRoutes
{
    /// <summary>Version prefix every module groups under.</summary>
    public const string VersionedPrefix = "api/v{version:apiVersion}";

    /// <summary>Absolute path of the SignalR hub.</summary>
    public const string RealtimeHub = "/api/v1/realtime/hub";

    /// <summary>Absolute path of the server sent events stream.</summary>
    public const string SseStream = "/api/v1/sse/stream";

    /// <summary>Absolute path of the server sent events token exchange.</summary>
    public const string SseToken = "/api/v1/sse/token";

    /// <summary>Paths excluded from rate limiting, quotas and security headers.</summary>
    public static readonly IReadOnlyList<string> Unmetered =
        ["/health", "/alive", "/ready", "/metrics", "/openapi", "/scalar"];
}
