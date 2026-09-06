using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dental.Framework.Persistence.Contexts;

/// <summary>
/// Base for every module <c>DbContext</c>. Marks every entity multi-tenant and applies the soft
/// delete filter, then hands off to Finbuckle to install the tenant query filter and to stamp
/// <c>TenantId</c> on save.
/// </summary>
/// <remarks>
/// <para>
/// A subclass overriding <see cref="OnModelCreating"/> MUST call
/// <c>base.OnModelCreating(modelBuilder)</c> as its LAST statement. Calling it first, or not at all,
/// silently drops the tenant filter for everything configured afterwards - the single most
/// dangerous mistake available in this codebase.
/// </para>
/// <para>
/// The tenant filter is owned by Finbuckle and stays anonymous. Only the soft delete filter is
/// named (<see cref="SoftDeleteFilterName"/>) so that a query can drop that one alone.
/// </para>
/// </remarks>
public abstract class BaseDbContext : MultiTenantDbContext
{
    /// <summary>Name of the soft delete query filter, so a query can drop just that one.</summary>
    public const string SoftDeleteFilterName = "SoftDelete";

    /// <summary>Creates the context.</summary>
    /// <param name="multiTenantContextAccessor">Finbuckle accessor supplying the ambient tenant.</param>
    /// <param name="options">EF options.</param>
    protected BaseDbContext(
        IMultiTenantContextAccessor multiTenantContextAccessor,
        DbContextOptions options)
        : base(multiTenantContextAccessor, options)
    {
    }

    /// <summary>Schema every entity in this context is mapped into. One schema per module.</summary>
    protected abstract string Schema { get; }

    /// <summary>Identifier of the ambient tenant, or <see langword="null"/> when none is resolved.</summary>
    public string? CurrentTenantId => TenantInfo?.Id;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyDentalConventions();

        // LAST: installs Finbuckle's anonymous tenant query filter over everything marked above.
        base.OnModelCreating(modelBuilder);
    }
}
