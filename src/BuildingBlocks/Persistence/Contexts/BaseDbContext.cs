using System.Linq.Expressions;
using System.Reflection;
using Dental.Framework.Core.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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

    private static readonly MethodInfo ApplySoftDeleteFilterMethod =
        typeof(BaseDbContext).GetMethod(
            nameof(ApplySoftDeleteFilterCore),
            BindingFlags.Public | BindingFlags.Static)!;

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

    /// <summary>
    /// Applies the soft delete filter to one entity type. Public only because it is invoked
    /// reflectively over an open generic; do not call it directly.
    /// </summary>
    /// <typeparam name="TEntity">The soft deletable entity type.</typeparam>
    /// <param name="modelBuilder">The model builder.</param>
    public static void ApplySoftDeleteFilterCore<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        Expression<Func<TEntity, bool>> filter = e => e.DeletedAt == null;
        modelBuilder.Entity<TEntity>().HasQueryFilter(SoftDeleteFilterName, filter);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);

        IEnumerable<Type> clrTypes = modelBuilder.Model.GetEntityTypes().Select(e => e.ClrType);

        foreach (Type clrType in clrTypes)
        {
            // Global entities - plans, outbox, inbox - deliberately carry no tenant filter.
            if (!clrType.IsAssignableTo(typeof(IGlobalEntity)) && clrType.IsAssignableTo(typeof(IHasTenant)))
            {
                modelBuilder.Entity(clrType).IsMultiTenant();
            }

            if (clrType.IsAssignableTo(typeof(ISoftDeletable)))
            {
                ApplySoftDeleteFilterMethod.MakeGenericMethod(clrType).Invoke(null, [modelBuilder]);
            }
        }

        // LAST: installs Finbuckle's anonymous tenant query filter over everything marked above.
        base.OnModelCreating(modelBuilder);
    }
}
