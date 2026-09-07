using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Contexts;
using Dental.Modules.Clinical.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Clinical.Data;

/// <summary>The clinical record: catalog, treatment plans and the tooth chart.</summary>
/// <param name="multiTenantContextAccessor">Finbuckle accessor.</param>
/// <param name="options">EF options.</param>
public sealed class ClinicalDbContext(
    IMultiTenantContextAccessor multiTenantContextAccessor,
    DbContextOptions<ClinicalDbContext> options)
    : BaseDbContext(multiTenantContextAccessor, options)
{
    /// <summary>Schema name for this module.</summary>
    public const string SchemaName = "clinical";

    /// <summary>The procedure catalog.</summary>
    public DbSet<Procedure> Procedures => Set<Procedure>();

    /// <summary>Treatment plans.</summary>
    public DbSet<TreatmentPlan> TreatmentPlans => Set<TreatmentPlan>();

    /// <summary>Treatment plan items.</summary>
    public DbSet<TreatmentPlanItem> TreatmentPlanItems => Set<TreatmentPlanItem>();

    /// <summary>Tooth chart entries.</summary>
    public DbSet<ChartEntry> ChartEntries => Set<ChartEntry>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicalDbContext).Assembly);
        modelBuilder.ApplyEventingModel();

        // LAST, always.
        base.OnModelCreating(modelBuilder);
    }
}
