using Microsoft.EntityFrameworkCore;

namespace Dental.Framework.Persistence.Pagination;

/// <summary>Turns a composed <see cref="IQueryable{T}"/> into a <see cref="PagedResponse{T}"/>.</summary>
public static class QueryablePagingExtensions
{
    /// <summary>
    /// Counts the matching rows and materializes the requested page in two round trips.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="query">The composed query. Must already be projected and ordered.</param>
    /// <param name="pagedQuery">The request carrying page number and size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page.</returns>
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query,
        IPagedQuery pagedQuery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(pagedQuery);

        int pageNumber = Math.Max(PaginationDefaults.MinPageNumber, pagedQuery.PageNumber);
        int pageSize = Math.Clamp(
            pagedQuery.PageSize <= 0 ? PaginationDefaults.DefaultPageSize : pagedQuery.PageSize,
            1,
            PaginationDefaults.MaxPageSize);

        int totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        if (totalCount == 0)
        {
            return PagedResponse.Empty<T>(pageNumber, pageSize);
        }

        List<T> items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<T>(items, pageNumber, pageSize, totalCount);
    }
}
