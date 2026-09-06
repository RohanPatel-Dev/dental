using Finbuckle.MultiTenant.Abstractions;

namespace Dental.Framework.Web.Tenancy;

/// <summary>
/// The tenant shape Finbuckle is configured with process-wide. The Tenancy module's store supplies
/// instances of it; everything else - including background work - only reads it.
/// </summary>
public sealed class DentalTenantInfo : ITenantInfo
{
    /// <inheritdoc />
    public string Id { get; set; } = default!;

    /// <inheritdoc />
    public string Identifier { get; set; } = default!;

    /// <summary>Display name of the practice.</summary>
    public string? Name { get; set; }

    /// <summary>Plan the tenant is subscribed to, which drives quota limits.</summary>
    public string? Plan { get; set; }

    /// <summary>Whether the tenant is allowed to sign in and use the API.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When the subscription lapses, if it does.</summary>
    public DateTimeOffset? ValidUntil { get; set; }

    /// <summary>IANA time zone the practice schedules in, e.g. <c>America/Chicago</c>.</summary>
    public string TimeZone { get; set; } = "UTC";
}
