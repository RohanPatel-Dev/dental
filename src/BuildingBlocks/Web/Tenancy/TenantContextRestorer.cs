using Dental.Framework.Core.Contracts;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;

namespace Dental.Framework.Web.Tenancy;

/// <summary>
/// Loads a tenant from the store and, separately, pushes it into Finbuckle's <c>AsyncLocal</c>.
/// </summary>
/// <remarks>
/// See <see cref="ITenantContextRestorer"/> for why the lookup and the assignment are two calls.
/// </remarks>
/// <param name="store">The tenant store.</param>
/// <param name="setter">Finbuckle's context setter.</param>
/// <param name="logger">Logger.</param>
public sealed class TenantContextRestorer(
    IMultiTenantStore<DentalTenantInfo> store,
    IMultiTenantContextSetter setter,
    ILogger<TenantContextRestorer> logger) : ITenantContextRestorer
{
    /// <inheritdoc />
    public async Task<TenantSnapshot?> ResolveAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        DentalTenantInfo? tenant = await store.GetAsync(tenantId).ConfigureAwait(false)
            ?? await store.GetByIdentifierAsync(tenantId).ConfigureAwait(false);

        if (tenant is null)
        {
            logger.LogWarning(
                "Tenant {TenantId} was not found in the store; background work will run without a "
                + "tenant context and tenant filtered queries will fail.",
                tenantId);
            return null;
        }

        return new TenantSnapshot(
            tenant.Id,
            tenant.Identifier,
            tenant.Name,
            tenant.Plan,
            tenant.TimeZone,
            tenant.IsActive,
            tenant.ValidUntil);
    }

    /// <inheritdoc />
    public void Apply(TenantSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        setter.MultiTenantContext = new MultiTenantContext<DentalTenantInfo>(
            new DentalTenantInfo
            {
                Id = snapshot.Id,
                Identifier = snapshot.Identifier,
                Name = snapshot.Name,
                Plan = snapshot.Plan,
                TimeZone = snapshot.TimeZone,
                IsActive = snapshot.IsActive,
                ValidUntil = snapshot.ValidUntil,
            });
    }
}
