using Dental.Framework.Persistence.Contexts;
using Dental.Modules.Auditing.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Auditing.Data;

/// <summary>The audit trail store.</summary>
/// <param name="multiTenantContextAccessor">Finbuckle accessor.</param>
/// <param name="options">EF options.</param>
public sealed class AuditingDbContext(
    IMultiTenantContextAccessor multiTenantContextAccessor,
    DbContextOptions<AuditingDbContext> options)
    : BaseDbContext(multiTenantContextAccessor, options)
{
    /// <summary>Schema name for this module.</summary>
    public const string SchemaName = "auditing";

    /// <summary>Recorded changes.</summary>
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditingDbContext).Assembly);

        // LAST, always.
        base.OnModelCreating(modelBuilder);
    }
}
