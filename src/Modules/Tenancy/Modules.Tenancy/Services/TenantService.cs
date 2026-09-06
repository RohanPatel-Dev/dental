using Dental.Framework.Caching;
using Dental.Framework.Shared.Caching;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Dental.Modules.Tenancy.Contracts.Services;
using Dental.Modules.Tenancy.Data;
using Dental.Modules.Tenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Modules.Tenancy.Services;

/// <summary>The Tenancy module's implementation of its own public contract.</summary>
/// <param name="context">The tenancy context.</param>
/// <param name="cache">Shared cache.</param>
public sealed class TenantService(TenancyDbContext context, HybridCache cache) : ITenantService
{
    /// <inheritdoc />
    public async Task<TenantDto?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return await cache.GetOrCreateAsync(
                CacheKeys.TenantKeys.ById(tenantId),
                async token =>
                {
                    Tenant? tenant = await context.Tenants
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Identifier == tenantId, token)
                        .ConfigureAwait(false);

                    return Map(tenant);
                },
                tags: [CacheKeys.Tags.Tenants],
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(
                CacheKeys.TenantKeys.List,
                async token =>
                {
                    List<Tenant> tenants = await context.Tenants
                        .AsNoTracking()
                        .OrderBy(t => t.Name)
                        .ToListAsync(token)
                        .ConfigureAwait(false);

                    return (IReadOnlyList<TenantDto>)[.. tenants.Select(Map).OfType<TenantDto>()];
                },
                tags: [CacheKeys.Tags.Tenants],
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TenantPlanDto?> GetPlanAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        TenantDto? tenant = await GetAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return null;
        }

        TenantPlan? plan = await context.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == tenant.Plan, cancellationToken)
            .ConfigureAwait(false);

        return plan is null
            ? null
            : new TenantPlanDto(
                plan.Name,
                plan.Description,
                new Dictionary<string, long>(StringComparer.Ordinal)
                {
                    ["ApiCalls"] = plan.ApiCallLimit,
                    ["StorageBytes"] = plan.StorageByteLimit,
                    ["Users"] = plan.UserLimit,
                    ["Patients"] = plan.PatientLimit,
                    ["Notifications"] = plan.NotificationLimit,
                });
    }

    internal static TenantDto? Map(Tenant? tenant) =>
        tenant is null
            ? null
            : new TenantDto(
                tenant.Identifier,
                tenant.Identifier,
                tenant.Name,
                tenant.PlanName,
                tenant.IsActive,
                tenant.ValidUntil,
                tenant.TimeZone,
                tenant.AdminEmail,
                tenant.CreatedAt);
}
