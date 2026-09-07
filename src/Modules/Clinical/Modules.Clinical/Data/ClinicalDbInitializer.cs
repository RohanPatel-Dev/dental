using Dental.Framework.Persistence.Initialization;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Clinical.Data;

/// <summary>Migrates the clinical schema and seeds a starter procedure catalog.</summary>
/// <param name="context">The clinical context.</param>
/// <param name="tenantContextAccessor">Supplies the tenant being seeded.</param>
/// <param name="logger">Logger.</param>
public sealed class ClinicalDbInitializer(
    ClinicalDbContext context,
    IMultiTenantContextAccessor tenantContextAccessor,
    ILogger<ClinicalDbInitializer> logger) : IDbInitializer
{
    /// <inheritdoc />
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false))
            .Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Applied pending migrations for the clinical schema.");
        }
    }

    /// <inheritdoc />
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Procedures.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        string tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id ?? string.Empty;

        // A short starter catalog of common CDT codes; a practice edits it from the admin console.
        (string Code, string Description, ProcedureCategory Category, decimal Fee, int Minutes)[] catalog =
        [
            ("D0120", "Periodic oral evaluation", ProcedureCategory.Diagnostic, 65m, 20),
            ("D0210", "Intraoral radiographs, complete series", ProcedureCategory.Diagnostic, 150m, 30),
            ("D1110", "Prophylaxis, adult", ProcedureCategory.Preventive, 110m, 45),
            ("D1206", "Topical fluoride varnish", ProcedureCategory.Preventive, 45m, 10),
            ("D2140", "Amalgam, one surface", ProcedureCategory.Restorative, 175m, 40),
            ("D2391", "Resin-based composite, one surface, posterior", ProcedureCategory.Restorative, 195m, 45),
            ("D2740", "Crown, porcelain/ceramic", ProcedureCategory.Restorative, 1250m, 90),
            ("D3310", "Endodontic therapy, anterior tooth", ProcedureCategory.Endodontic, 900m, 90),
            ("D4341", "Periodontal scaling and root planing, per quadrant", ProcedureCategory.Periodontic, 280m, 60),
            ("D7140", "Extraction, erupted tooth", ProcedureCategory.Surgical, 220m, 40),
        ];

        foreach ((string code, string description, ProcedureCategory category, decimal fee, int minutes)
                 in catalog)
        {
            context.Procedures.Add(new Procedure
            {
                Code = code,
                Description = description,
                Category = category,
                DefaultFee = fee,
                Currency = "USD",
                DefaultDurationMinutes = minutes,
                IsActive = true,
                TenantId = tenantId,
            });
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Seeded {Count} catalog procedures.", catalog.Length);
    }
}
