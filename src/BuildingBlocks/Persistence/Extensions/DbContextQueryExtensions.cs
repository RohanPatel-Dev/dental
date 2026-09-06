using Dental.Framework.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Dental.Framework.Persistence.Extensions;

/// <summary>Helpers that make the dangerous query shapes explicit at the call site.</summary>
public static class DbContextQueryExtensions
{
    /// <summary>
    /// Drops only the named soft delete filter, keeping tenant isolation intact.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <returns>The query including soft deleted rows.</returns>
    public static IQueryable<T> IncludingSoftDeleted<T>(this IQueryable<T> query)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        return query.IgnoreQueryFilters([BaseDbContext.SoftDeleteFilterName]);
    }

    /// <summary>
    /// Drops every query filter and immediately re-applies an explicit tenant predicate.
    /// </summary>
    /// <typeparam name="T">Entity type implementing <see cref="Core.Domain.IHasTenant"/>.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="tenantId">Tenant to restrict the query to.</param>
    /// <returns>The explicitly filtered query.</returns>
    /// <remarks>
    /// Cross-tenant reads must never rely on the mere absence of the filter. This helper exists so
    /// that "ignore filters" and "re-filter" cannot be separated by a later edit.
    /// </remarks>
    public static IQueryable<T> ForTenantIgnoringFilters<T>(this IQueryable<T> query, string tenantId)
        where T : class, Core.Domain.IHasTenant
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return query.IgnoreQueryFilters().Where(e => e.TenantId == tenantId);
    }
}
