using Dental.Framework.Web.Tenancy;
using Dental.Modules.Tenancy.Data;
using Dental.Modules.Tenancy.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dental.Modules.Tenancy.Services;

/// <summary>
/// Finbuckle tenant store backed by the tenant catalog.
/// </summary>
/// <remarks>
/// Finbuckle resolves the tenant before authentication and outside any request scope in background
/// work, so the store creates its own scope rather than capturing a scoped <c>DbContext</c>.
/// </remarks>
/// <param name="scopeFactory">Creates a scope per lookup.</param>
public sealed class EfTenantStore(IServiceScopeFactory scopeFactory) : IMultiTenantStore<DentalTenantInfo>
{
    /// <inheritdoc />
    public async Task<DentalTenantInfo?> GetAsync(string id)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        TenancyDbContext context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        Tenant? tenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Identifier == id)
            .ConfigureAwait(false);

        return Map(tenant);
    }

    /// <inheritdoc />
    public Task<DentalTenantInfo?> GetByIdentifierAsync(string identifier) => GetAsync(identifier);

    /// <inheritdoc />
    public async Task<IEnumerable<DentalTenantInfo>> GetAllAsync()
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        TenancyDbContext context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        List<Tenant> tenants = await context.Tenants.AsNoTracking().ToListAsync().ConfigureAwait(false);
        return tenants.Select(Map).OfType<DentalTenantInfo>();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DentalTenantInfo>> GetAllAsync(int take, int skip)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        TenancyDbContext context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

        List<Tenant> tenants = await context.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Identifier)
            .Skip(skip)
            .Take(take)
            .ToListAsync()
            .ConfigureAwait(false);

        return tenants.Select(Map).OfType<DentalTenantInfo>();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Writes go through the module's commands, never through Finbuckle: creating a tenant also
    /// writes the outbox row that lets other modules seed themselves, and that must happen in the
    /// command's transaction.
    /// </remarks>
    public Task<bool> AddAsync(DentalTenantInfo tenantInfo) =>
        throw new NotSupportedException(
            "Tenants are created through CreateTenantCommand, which also raises "
            + "TenantProvisionedIntegrationEvent in the same transaction.");

    /// <inheritdoc />
    public Task<bool> UpdateAsync(DentalTenantInfo tenantInfo) =>
        throw new NotSupportedException(
            "Tenants are updated through SetTenantStatusCommand and ChangeTenantPlanCommand.");

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string identifier) =>
        throw new NotSupportedException(
            "Tenants are never removed; they are deactivated through SetTenantStatusCommand.");

    private static DentalTenantInfo? Map(Tenant? tenant) =>
        tenant is null
            ? null
            : new DentalTenantInfo
            {
                Id = tenant.Identifier,
                Identifier = tenant.Identifier,
                Name = tenant.Name,
                Plan = tenant.PlanName,
                IsActive = tenant.IsActive,
                ValidUntil = tenant.ValidUntil,
                TimeZone = tenant.TimeZone,
            };
}
