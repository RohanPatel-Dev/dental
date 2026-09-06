using System.Linq.Expressions;
using System.Reflection;
using Dental.Framework.Core.Domain;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Dental.Framework.Persistence.Contexts;

/// <summary>
/// The conventions every module context applies: tenant isolation by default, opt-out via
/// <see cref="IGlobalEntity"/>, and a NAMED soft delete filter.
/// </summary>
/// <remarks>
/// Extracted so that <see cref="BaseDbContext"/> and any context that cannot derive from it - the
/// Identity context, which must derive from <c>IdentityDbContext</c> - apply exactly the same rules.
/// </remarks>
public static class ModelConventions
{
    private static readonly MethodInfo ApplySoftDeleteFilterMethod =
        typeof(ModelConventions).GetMethod(
            nameof(ApplySoftDeleteFilter),
            BindingFlags.Public | BindingFlags.Static)!;

    /// <summary>
    /// Marks every tenant scoped entity multi-tenant and applies the soft delete filter.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <remarks>
    /// Call this immediately BEFORE the final <c>base.OnModelCreating(modelBuilder)</c>, and note
    /// that installing Finbuckle's filter is the job of <c>MultiTenantDbContext</c> or of an
    /// explicit <c>ConfigureMultiTenant()</c> call afterwards.
    /// </remarks>
    public static void ApplyDentalConventions(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        IEnumerable<Type> clrTypes = modelBuilder.Model.GetEntityTypes().Select(e => e.ClrType);

        foreach (Type clrType in clrTypes)
        {
            // Global entities - plans, outbox, inbox, the tenant catalog - carry no tenant filter.
            if (!clrType.IsAssignableTo(typeof(IGlobalEntity)) && clrType.IsAssignableTo(typeof(IHasTenant)))
            {
                modelBuilder.Entity(clrType).IsMultiTenant();
            }

            if (clrType.IsAssignableTo(typeof(ISoftDeletable)))
            {
                ApplySoftDeleteFilterMethod.MakeGenericMethod(clrType).Invoke(null, [modelBuilder]);
            }
        }
    }

    /// <summary>
    /// Applies the named soft delete filter to one entity type. Public only because it is invoked
    /// reflectively over an open generic; do not call it directly.
    /// </summary>
    /// <typeparam name="TEntity">The soft deletable entity type.</typeparam>
    /// <param name="modelBuilder">The model builder.</param>
    public static void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        Expression<Func<TEntity, bool>> filter = e => e.DeletedAt == null;
        modelBuilder.Entity<TEntity>().HasQueryFilter(BaseDbContext.SoftDeleteFilterName, filter);
    }
}
