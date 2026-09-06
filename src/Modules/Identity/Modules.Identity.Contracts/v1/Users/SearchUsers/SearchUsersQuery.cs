using Dental.Framework.Shared.Pagination;
using Dental.Modules.Identity.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Identity.Contracts.v1.Users.SearchUsers;

/// <summary>Pages through the users of the current tenant.</summary>
/// <param name="SearchTerm">Matched against email and name.</param>
/// <param name="Role">Filters by role name.</param>
/// <param name="IsActive">Filters by activation state.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchUsersQuery(
    string? SearchTerm,
    string? Role,
    bool? IsActive,
    int PageNumber,
    int PageSize,
    string? Sort) : IQuery<PagedResponse<UserDto>>, IPagedQuery;
