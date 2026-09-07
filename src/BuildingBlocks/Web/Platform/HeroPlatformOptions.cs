namespace Dental.Framework.Web.Platform;

/// <summary>Feature flags for <c>AddHeroPlatform</c>.</summary>
public sealed class HeroPlatformOptions
{
/*
 * Caching, storage, mailing, quotas and realtime SERVICES are registered in every host and have no
 * flag: feature code depends on those abstractions unconditionally, and a handler cannot know which
 * host loaded it. The flags below turn off expensive or outward-facing behaviour instead.
 */

    /// <summary>
    /// Runs a Hangfire client and server. When false the host gets a job service whose every method
    /// throws, so an accidental enqueue from a process that runs no worker fails loudly.
    /// </summary>
    public bool EnableJobs { get; set; }

    /// <summary>
    /// Enforces per-tenant quotas. When false the quota service answers unlimited and the
    /// enforcement middleware is not added.
    /// </summary>
    public bool EnableQuotas { get; set; }

    /// <summary>Registers the server-sent-events plumbing.</summary>
    public bool EnableSse { get; set; }

    /// <summary>Registers the rate limiter.</summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>Queue name prefix for the RabbitMQ consumer. Defaults to the application name.</summary>
    public string? ConsumerName { get; set; }
}

/// <summary>Feature flags for <c>UseHeroPlatform</c>.</summary>
public sealed class HeroPipelineOptions
{
    /// <summary>Serves static files from wwwroot.</summary>
    public bool UseStaticFiles { get; set; }

    /// <summary>Maps the OpenAPI document and the Scalar reference UI.</summary>
    public bool MapOpenApi { get; set; } = true;

    /// <summary>
    /// Maps the SignalR hub. A headless host registers SignalR services but leaves this off, and
    /// still reaches browsers through the shared Redis backplane.
    /// </summary>
    public bool MapRealtime { get; set; } = true;

    /// <summary>Maps the server-sent-events endpoints.</summary>
    public bool MapSse { get; set; } = true;

    /// <summary>Maps the Hangfire dashboard.</summary>
    public bool MapJobsDashboard { get; set; } = true;

    /// <summary>Adds the idempotent-replay middleware.</summary>
    public bool UseIdempotency { get; set; } = true;

    /// <summary>Redirects HTTP to HTTPS. Off behind a TLS terminating proxy.</summary>
    public bool UseHttpsRedirection { get; set; } = true;
}
