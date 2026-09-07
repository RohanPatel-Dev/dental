using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Contexts;
using Dental.Modules.Patients.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Patients.Data;

/// <summary>The patient register.</summary>
/// <param name="multiTenantContextAccessor">Finbuckle accessor.</param>
/// <param name="options">EF options.</param>
public sealed class PatientsDbContext(
    IMultiTenantContextAccessor multiTenantContextAccessor,
    DbContextOptions<PatientsDbContext> options)
    : BaseDbContext(multiTenantContextAccessor, options)
{
    /// <summary>Schema name for this module.</summary>
    public const string SchemaName = "patients";

    /// <summary>Patient records.</summary>
    public DbSet<Patient> Patients => Set<Patient>();

    /// <summary>Files attached to patient records.</summary>
    public DbSet<PatientDocument> Documents => Set<PatientDocument>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientsDbContext).Assembly);
        modelBuilder.ApplyEventingModel();

        // LAST, always.
        base.OnModelCreating(modelBuilder);
    }
}
