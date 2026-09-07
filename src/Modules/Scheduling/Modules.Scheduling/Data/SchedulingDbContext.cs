using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Contexts;
using Dental.Modules.Scheduling.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Scheduling.Data;

/// <summary>The appointment book.</summary>
/// <param name="multiTenantContextAccessor">Finbuckle accessor.</param>
/// <param name="options">EF options.</param>
public sealed class SchedulingDbContext(
    IMultiTenantContextAccessor multiTenantContextAccessor,
    DbContextOptions<SchedulingDbContext> options)
    : BaseDbContext(multiTenantContextAccessor, options)
{
    /// <summary>Schema name for this module.</summary>
    public const string SchemaName = "scheduling";

    /// <summary>Booked appointments.</summary>
    public DbSet<Appointment> Appointments => Set<Appointment>();

    /// <summary>Clinicians.</summary>
    public DbSet<Provider> Providers => Set<Provider>();

    /// <summary>Treatment rooms.</summary>
    public DbSet<Operatory> Operatories => Set<Operatory>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
        modelBuilder.ApplyEventingModel();

        // LAST, always.
        base.OnModelCreating(modelBuilder);
    }
}
