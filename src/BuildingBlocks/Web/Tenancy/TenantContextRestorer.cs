using Dental.Framework.Core.Contracts;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;

namespace Dental.Framework.Web.Tenancy;

/// <summary>
/// Re-establishes the ambient tenant for background work, by loading it from the tenant store and
/// pushing it into Finbuckle's setter.
/// </summary>
/// <remarks>
/// Finbuckle's context is AsyncLocal. Setting it inside an awaited helper that returns before the
/// caller continues can lose it across the async boundary - which is why callers await this
/// directly rather than fire-and-forget.
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
    public async Task RestoreAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        DentalTenantInfo? tenant = await store.GetAsync(tenantId).ConfigureAwait(false)
            ?? await store.GetByIdentifierAsync(tenantId).ConfigureAwait(false);

        if (tenant is null)
        {
            logger.LogWarning(
                "Tenant {TenantId} was not found in the store; background work will run without "
                + "a tenant context and tenant filtered queries will fail.",
                tenantId);
            return;
        }

        setter.MultiTenantContext = new MultiTenantContext<DentalTenantInfo>(tenant);
    }
}
