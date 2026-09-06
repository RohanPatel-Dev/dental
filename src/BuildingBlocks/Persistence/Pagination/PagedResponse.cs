namespace Dental.Framework.Persistence.Pagination;

/// <summary>Standard envelope for every paginated query response.</summary>
/// <typeparam name="T">Item type.</typeparam>
/// <param name="Items">The page of items.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size that was applied.</param>
/// <param name="TotalCount">Total matching rows across all pages.</param>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    /// <summary>Number of pages available at the current page size.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>True when a previous page exists.</summary>
    public bool HasPrevious => PageNumber > 1;

    /// <summary>True when a further page exists.</summary>
    public bool HasNext => PageNumber < TotalPages;
}

/// <summary>Factories for <see cref="PagedResponse{T}"/>.</summary>
public static class PagedResponse
{
    /// <summary>An empty page, useful for short circuiting a query.</summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>An empty response.</returns>
    public static PagedResponse<T> Empty<T>(int pageNumber, int pageSize) =>
        new([], pageNumber, pageSize, 0);
}
