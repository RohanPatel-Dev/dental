namespace Dental.Framework.Shared.Pagination;

/// <summary>Shared pagination bounds. Validators reference these instead of hard coding numbers.</summary>
public static class PaginationDefaults
{
    /// <summary>Page size applied when the caller does not ask for one.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Largest page a caller may request.</summary>
    public const int MaxPageSize = 200;

    /// <summary>Smallest valid page number.</summary>
    public const int MinPageNumber = 1;
}
