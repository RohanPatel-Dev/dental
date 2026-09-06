using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Contexts;
using Dental.Modules.Tenancy.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Tenancy.Data;

/// <summary>The tenant catalog. Everything in it is global by design.</summary>
/// <param name="multiTenantContextAccessor">Finbuckle accessor.</param>
/// <param name="options">EF options.</param>
public sealed class TenancyDbContext(
    IMultiTenantContextAccessor multiTenantContextAccessor,
    DbContextOptions<TenancyDbContext> options)
    : BaseDbContext(multiTenantContextAccessor, options)
{
    /// <summary>Schema name for this module.</summary>
    public const string SchemaName = "tenancy";

    /// <summary>The tenant catalog.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Subscription plans.</summary>
    public DbSet<TenantPlan> Plans => Set<TenantPlan>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenancyDbContext).Assembly);
        modelBuilder.ApplyEventingModel();

        // LAST, always. Anything configured after this call loses its tenant filter.
        base.OnModelCreating(modelBuilder);
    }
}
