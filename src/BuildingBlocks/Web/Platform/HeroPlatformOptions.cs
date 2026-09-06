namespace Dental.Framework.Web.Platform;

/// <summary>Feature flags for <c>AddHeroPlatform</c>.</summary>
public sealed class HeroPlatformOptions
{
    /// <summary>Registers <c>HybridCache</c> and, when configured, its Redis L2.</summary>
    public bool EnableCaching { get; set; }

    /// <summary>Registers the SMTP mail service.</summary>
    public bool EnableMailing { get; set; }

    /// <summary>Registers Hangfire's client and server.</summary>
    public bool EnableJobs { get; set; }

    /// <summary>Registers the quota service and enables the enforcement middleware.</summary>
    public bool EnableQuotas { get; set; }

    /// <summary>Registers the server-sent-events plumbing.</summary>
    public bool EnableSse { get; set; }

    /// <summary>Registers SignalR services. Mapping the hub is a separate flag.</summary>
    public bool EnableRealtime { get; set; }

    /// <summary>Registers object storage.</summary>
    public bool EnableStorage { get; set; } = true;

    /// <summary>Registers the rate limiter.</summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>Registers the idempotency middleware. Requires caching.</summary>
    public bool EnableIdempotency { get; set; } = true;

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

    /// <summary>Redirects HTTP to HTTPS. Off behind a TLS terminating proxy.</summary>
    public bool UseHttpsRedirection { get; set; } = true;
}
