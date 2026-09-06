using Dental.Modules.Tenancy.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Tenancy.Contracts.v1.Tenants.GetTenant;

/// <summary>Reads one tenant from the catalog.</summary>
/// <param name="Identifier">The tenant slug.</param>
public sealed record GetTenantQuery(string Identifier) : IQuery<TenantDto>;
