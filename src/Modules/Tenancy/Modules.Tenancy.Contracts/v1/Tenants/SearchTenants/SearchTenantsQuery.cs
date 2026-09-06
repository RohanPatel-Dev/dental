using Dental.Framework.Shared.Pagination;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Tenancy.Contracts.v1.Tenants.SearchTenants;

/// <summary>Pages through the tenant catalog.</summary>
/// <param name="SearchTerm">Matched against the identifier and the practice name.</param>
/// <param name="IsActive">Filters by activation state when supplied.</param>
/// <param name="Plan">Filters by plan when supplied.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression, e.g. <c>name asc</c>.</param>
public sealed record SearchTenantsQuery(
    string? SearchTerm,
    bool? IsActive,
    string? Plan,
    int PageNumber,
    int PageSize,
    string? Sort) : IQuery<PagedResponse<TenantDto>>, IPagedQuery;
