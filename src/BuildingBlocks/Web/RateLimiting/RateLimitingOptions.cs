using System.ComponentModel.DataAnnotations;

namespace Dental.Framework.Web.RateLimiting;

/// <summary>Rate limiting configuration, bound from the <c>RateLimitingOptions</c> section.</summary>
/// <remarks>
/// <see cref="Enabled"/> is read EAGERLY at registration time. A test that only overlays
/// configuration will still get whatever was decided at startup.
/// </remarks>
public sealed class RateLimitingOptions
{
    /// <summary>Whether rate limiting is applied at all.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Window length in seconds, shared by all three partitions.</summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Requests per window per tenant.</summary>
    [Range(1, 1000000)]
    public int TenantPermitLimit { get; set; } = 1000;

    /// <summary>Requests per window per authenticated user.</summary>
    [Range(1, 1000000)]
    public int UserPermitLimit { get; set; } = 200;

    /// <summary>Requests per window per client IP address.</summary>
    [Range(1, 1000000)]
    public int IpPermitLimit { get; set; } = 300;

    /// <summary>Requests per window against the strict authentication endpoints.</summary>
    [Range(1, 10000)]
    public int AuthPermitLimit { get; set; } = 10;
}
