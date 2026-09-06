using Dental.Framework.Persistence.Initialization;
using Dental.Framework.Shared.Tenancy;
using Dental.Modules.Tenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Tenancy.Data;

/// <summary>Migrates the tenant catalog and seeds the plans plus the root tenant.</summary>
/// <param name="context">The tenancy context.</param>
/// <param name="logger">Logger.</param>
public sealed class TenancyDbInitializer(TenancyDbContext context, ILogger<TenancyDbInitializer> logger)
    : IDbInitializer
{
    /// <inheritdoc />
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false))
            .Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Applied pending migrations for the tenant catalog.");
        }
    }

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPlansAsync(cancellationToken).ConfigureAwait(false);
        await SeedRootTenantAsync(cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedPlansAsync(CancellationToken cancellationToken)
    {
        TenantPlan[] plans =
        [
            new()
            {
                Name = TenantPlans.Solo,
                Description = "A single practitioner and up to five staff accounts.",
                ApiCallLimit = 100_000,
                StorageByteLimit = 10L * 1024 * 1024 * 1024,
                UserLimit = 5,
                PatientLimit = 2_500,
                NotificationLimit = 5_000,
                TenantId = TenantConstants.RootTenant,
            },
            new()
            {
                Name = TenantPlans.Practice,
                Description = "A multi-chair practice with a full front desk.",
                ApiCallLimit = 500_000,
                StorageByteLimit = 100L * 1024 * 1024 * 1024,
                UserLimit = 50,
                PatientLimit = 25_000,
                NotificationLimit = 50_000,
                TenantId = TenantConstants.RootTenant,
            },
            new()
            {
                Name = TenantPlans.Group,
                Description = "A multi-location dental group.",
                ApiCallLimit = 5_000_000,
                StorageByteLimit = 1024L * 1024 * 1024 * 1024,
                UserLimit = 500,
                PatientLimit = 250_000,
                NotificationLimit = 500_000,
                TenantId = TenantConstants.RootTenant,
            },
        ];

        HashSet<string> existing = await context.Plans
            .AsNoTracking()
            .Select(p => p.Name)
            .ToHashSetAsync(StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        foreach (TenantPlan plan in plans.Where(p => !existing.Contains(p.Name)))
        {
            context.Plans.Add(plan);
            logger.LogInformation("Seeded tenant plan {PlanName}.", plan.Name);
        }
    }

    private async Task SeedRootTenantAsync(CancellationToken cancellationToken)
    {
        bool exists = await context.Tenants
            .AnyAsync(t => t.Identifier == TenantConstants.RootTenant, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return;
        }

        context.Tenants.Add(new Tenant
        {
            Identifier = TenantConstants.RootTenant,
            Name = "Operator",
            AdminEmail = "operator@dental.local",
            PlanName = TenantPlans.Group,
            IsActive = true,
            TimeZone = "UTC",
            TenantId = TenantConstants.RootTenant,
        });

        logger.LogInformation("Seeded the root operator tenant.");
    }
}

/// <summary>Built in plan names.</summary>
public static class TenantPlans
{
    /// <summary>A single practitioner.</summary>
    public const string Solo = "solo";

    /// <summary>A multi-chair practice.</summary>
    public const string Practice = "practice";

    /// <summary>A multi-location group.</summary>
    public const string Group = "group";
}
