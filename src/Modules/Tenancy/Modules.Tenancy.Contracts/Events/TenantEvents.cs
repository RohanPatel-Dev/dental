using Dental.Framework.Eventing.Abstractions;

namespace Dental.Modules.Tenancy.Contracts.Events;

/// <summary>
/// Raised once a tenant has been provisioned, so modules can seed their own per-tenant defaults.
/// </summary>
/// <remarks>
/// The tenant identifier travels in <see cref="IIntegrationEvent.TenantId"/> on the base record, so
/// it is not repeated here. Do NOT rename or move this type: the outbox stores its assembly
/// qualified name, so a rename makes every queued row undeserializable and dead lettered.
/// Introduce a new type instead.
/// </remarks>
/// <param name="Identifier">The tenant's slug.</param>
/// <param name="Name">Practice name.</param>
/// <param name="AdminEmail">Address the first administrator account is created for.</param>
public sealed record TenantProvisionedIntegrationEvent(
    string Identifier,
    string Name,
    string AdminEmail) : IntegrationEvent;

/// <summary>Raised when a tenant is deactivated, so modules can stop scheduled work for it.</summary>
/// <param name="Reason">Why it was deactivated.</param>
public sealed record TenantDeactivatedIntegrationEvent(string Reason) : IntegrationEvent;
