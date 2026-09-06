using Dental.Framework.Quota;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Dental.Modules.Tenancy.Contracts.Services;

namespace Dental.Modules.Tenancy.Services;

/// <summary>
/// Supplies quota limits from the tenant's subscription plan, overriding the configured defaults.
/// </summary>
/// <param name="tenantService">Reads the tenant's plan.</param>
public sealed class PlanQuotaLimitProvider(ITenantService tenantService) : IQuotaLimitProvider
{
    /// <inheritdoc />
    public async Task<long?> GetLimitAsync(
        string tenantId,
        QuotaResource resource,
        CancellationToken cancellationToken = default)
    {
        TenantPlanDto? plan = await tenantService.GetPlanAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null)
        {
            return null;
        }

        return plan.Limits.TryGetValue(resource.ToString(), out long limit) ? limit : null;
    }
}
