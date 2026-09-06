namespace Dental.Modules.Tenancy.Contracts.Dtos;

/// <summary>A tenant as other modules and the operator console see it.</summary>
/// <param name="Id">Stable identifier, used as the tenant header value.</param>
/// <param name="Identifier">Human readable slug.</param>
/// <param name="Name">Practice name.</param>
/// <param name="Plan">Subscription plan driving quota limits.</param>
/// <param name="IsActive">Whether the tenant may sign in and use the API.</param>
/// <param name="ValidUntil">When the subscription lapses, if it does.</param>
/// <param name="TimeZone">IANA time zone the practice schedules in.</param>
/// <param name="AdminEmail">Contact address for the tenant's administrator.</param>
/// <param name="CreatedAt">When the tenant was provisioned.</param>
public sealed record TenantDto(
    string Id,
    string Identifier,
    string Name,
    string Plan,
    bool IsActive,
    DateTimeOffset? ValidUntil,
    string TimeZone,
    string AdminEmail,
    DateTimeOffset CreatedAt);

/// <summary>A subscription plan and the limits it grants.</summary>
/// <param name="Name">Plan name.</param>
/// <param name="Description">What the plan includes.</param>
/// <param name="Limits">Quota limits by resource name. A missing entry falls back to the default.</param>
public sealed record TenantPlanDto(
    string Name,
    string Description,
    IReadOnlyDictionary<string, long> Limits);
