using Dental.Modules.Tenancy.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Tenancy.Contracts.v1.Tenants.ChangeTenantPlan;

/// <summary>Moves a tenant onto a different subscription plan.</summary>
/// <param name="Identifier">The tenant slug.</param>
/// <param name="Plan">The new plan name.</param>
public sealed record ChangeTenantPlanCommand(string Identifier, string Plan) : ICommand<TenantDto>;
