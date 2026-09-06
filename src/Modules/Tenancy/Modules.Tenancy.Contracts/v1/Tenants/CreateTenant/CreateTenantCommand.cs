using Dental.Modules.Tenancy.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Tenancy.Contracts.v1.Tenants.CreateTenant;

/// <summary>Provisions a new practice.</summary>
/// <param name="Identifier">Slug used as the tenant header value. Lower case, unique.</param>
/// <param name="Name">Practice name.</param>
/// <param name="AdminEmail">Address the first administrator account is created for.</param>
/// <param name="Plan">Subscription plan name.</param>
/// <param name="TimeZone">IANA time zone the practice schedules in.</param>
/// <param name="ValidUntil">When the subscription lapses, if it does.</param>
public sealed record CreateTenantCommand(
    string Identifier,
    string Name,
    string AdminEmail,
    string Plan,
    string TimeZone,
    DateTimeOffset? ValidUntil) : ICommand<TenantDto>;
