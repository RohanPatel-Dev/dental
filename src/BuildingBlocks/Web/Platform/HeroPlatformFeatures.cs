namespace Dental.Framework.Web.Platform;

/// <summary>
/// What <c>AddHeroPlatform</c> actually turned on, recorded so that <c>UseHeroPlatform</c> can build
/// the matching pipeline.
/// </summary>
/// <remarks>
/// The pipeline used to probe the container - <c>app.Services.GetService&lt;IQuotaService&gt;()</c> -
/// to decide whether to add a middleware. That throws for any SCOPED service, because the root
/// provider has no scope. Recording the decision at registration time is both correct and clearer:
/// the pipeline reads a fact rather than inferring one.
/// </remarks>
public sealed class HeroPlatformFeatures
{
    /// <summary>Whether Hangfire's client and server are running in this host.</summary>
    public required bool Jobs { get; init; }

    /// <summary>Whether per-tenant quotas are enforced in this host.</summary>
    public required bool Quotas { get; init; }

    /// <summary>Whether the server-sent-events plumbing is registered.</summary>
    public required bool Sse { get; init; }

    /// <summary>Whether the rate limiter is registered.</summary>
    public required bool RateLimiting { get; init; }
}
