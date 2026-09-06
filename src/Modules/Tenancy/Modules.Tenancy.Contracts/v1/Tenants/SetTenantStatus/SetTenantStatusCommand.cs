using Dental.Modules.Tenancy.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Tenancy.Contracts.v1.Tenants.SetTenantStatus;

/// <summary>Activates or deactivates a tenant.</summary>
/// <param name="Identifier">The tenant slug.</param>
/// <param name="IsActive">Target state.</param>
/// <param name="Reason">Why the change is being made. Required when deactivating.</param>
public sealed record SetTenantStatusCommand(string Identifier, bool IsActive, string? Reason)
    : ICommand<TenantDto>;
