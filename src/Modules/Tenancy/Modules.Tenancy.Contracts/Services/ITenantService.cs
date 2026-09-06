using Dental.Modules.Tenancy.Contracts.Dtos;

namespace Dental.Modules.Tenancy.Contracts.Services;

/// <summary>
/// The Tenancy module's public surface. Other modules inject THIS - never the module's DbContext,
/// entities or handlers.
/// </summary>
public interface ITenantService
{
    /// <summary>Reads one tenant.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant, or null when it does not exist.</returns>
    Task<TenantDto?> GetAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Reads every tenant. Operator use only.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All tenants.</returns>
    Task<IReadOnlyList<TenantDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Reads the plan a tenant is subscribed to.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The plan, or null when the tenant or plan is unknown.</returns>
    Task<TenantPlanDto?> GetPlanAsync(string tenantId, CancellationToken cancellationToken = default);
}
