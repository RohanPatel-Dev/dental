using Dental.Framework.Persistence.Initialization;
using Dental.Framework.Shared.Tenancy;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dental.Modules.Identity.Data;

/// <summary>Migrates the identity schema and seeds roles for the current tenant.</summary>
/// <param name="context">The identity context.</param>
/// <param name="seeder">Seeds roles and the administrator.</param>
/// <param name="tenantContextAccessor">Supplies the tenant being migrated.</param>
/// <param name="seedOptions">Seeding configuration.</param>
/// <param name="logger">Logger.</param>
public sealed class IdentityDbInitializer(
    IdentityModuleDbContext context,
    IdentitySeeder seeder,
    IMultiTenantContextAccessor tenantContextAccessor,
    IOptions<IdentitySeedOptions> seedOptions,
    ILogger<IdentityDbInitializer> logger) : IDbInitializer
{
    /// <inheritdoc />
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false))
            .Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Applied pending migrations for the identity schema.");
        }
    }

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        string tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? TenantConstants.RootTenant;

        await seeder.SeedRolesAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (string.Equals(tenantId, TenantConstants.RootTenant, StringComparison.Ordinal))
        {
            await seeder
                .SeedAdministratorAsync(
                    tenantId,
                    seedOptions.Value.OperatorEmail,
                    "Operator",
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
