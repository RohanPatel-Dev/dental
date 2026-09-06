using Dental.Framework.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Dental.Framework.Persistence.Specifications;

/// <summary>Applies a <see cref="Specification{T}"/> to an <see cref="IQueryable{T}"/>.</summary>
public static class SpecificationEvaluator
{
    /// <summary>Composes the specification onto the query.</summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="inputQuery">The source query, usually a <c>DbSet</c>.</param>
    /// <param name="specification">The specification to apply.</param>
    /// <returns>The composed query.</returns>
    public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, Specification<T> specification)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(inputQuery);
        ArgumentNullException.ThrowIfNull(specification);

        IQueryable<T> query = inputQuery;

        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (specification.IncludeSoftDeleted)
        {
            // Drops ONLY the named soft delete filter. The tenant filter stays anonymous and stays on.
            query = query.IgnoreQueryFilters([BaseDbContext.SoftDeleteFilterName]);
        }

        query = specification.Criteria.Aggregate(query, (current, criteria) => current.Where(criteria));
        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }
}
