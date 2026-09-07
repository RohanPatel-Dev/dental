using Dental.Framework.Persistence.Initialization;
using Dental.Modules.Scheduling.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Scheduling.Data;

/// <summary>Migrates the scheduling schema and seeds a default set of chairs.</summary>
/// <param name="context">The scheduling context.</param>
/// <param name="tenantContextAccessor">Supplies the tenant being seeded.</param>
/// <param name="logger">Logger.</param>
public sealed class SchedulingDbInitializer(
    SchedulingDbContext context,
    IMultiTenantContextAccessor tenantContextAccessor,
    ILogger<SchedulingDbInitializer> logger) : IDbInitializer
{
    /// <inheritdoc />
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false))
            .Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Applied pending migrations for the scheduling schema.");
        }
    }

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Operatories.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        string tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id ?? string.Empty;

        // A practice cannot book anything without at least one chair, so this is required seed data
        // rather than demo data.
        for (int chair = 1; chair <= 2; chair++)
        {
            context.Operatories.Add(new Operatory
            {
                Name = $"Surgery {chair}",
                IsActive = true,
                TenantId = tenantId,
            });
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Seeded default operatories for tenant {TenantId}.", tenantId);
    }

    /// <inheritdoc />
    public async Task SeedDemoAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Providers.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        string tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id ?? string.Empty;

        (string Name, string Speciality)[] providers =
        [
            ("Dr Amara Nwosu", "General dentistry"),
            ("Dr Elias Hoffmann", "Endodontics"),
            ("Sian Roberts", "Dental hygiene"),
        ];

        foreach ((string name, string speciality) in providers)
        {
            context.Providers.Add(new Provider
            {
                DisplayName = name,
                Speciality = speciality,
                IsAcceptingPatients = true,
                TenantId = tenantId,
            });
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Seeded {Count} demo providers.", providers.Length);
    }
}
