namespace Dental.Framework.Shared.Pagination;

/// <summary>
/// Implemented by every paginated query. The architecture tests require a matching validator for
/// each one, so page size can never be unbounded.
/// </summary>
public interface IPagedQuery
{
    /// <summary>1-based page number.</summary>
    int PageNumber { get; }

    /// <summary>Requested page size.</summary>
    int PageSize { get; }

    /// <summary>Sort expression, e.g. <c>lastName asc</c>. Interpreted by the handler.</summary>
    string? Sort { get; }
}
