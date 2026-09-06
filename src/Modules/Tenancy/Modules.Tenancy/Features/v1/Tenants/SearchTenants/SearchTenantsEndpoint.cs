using Dental.Framework.Shared.Pagination;
using Dental.Framework.Web.Auth;
using Dental.Modules.Tenancy.Contracts.Authorization;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Dental.Modules.Tenancy.Contracts.v1.Tenants.SearchTenants;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.SearchTenants;

/// <summary>Maps <c>GET /tenants</c>.</summary>
public static class SearchTenantsEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapSearchTenantsEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/tenants",
                (
                    [AsParameters] SearchTenantsRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new SearchTenantsQuery(
                            request.SearchTerm,
                            request.IsActive,
                            request.Plan,
                            request.PageNumber ?? PaginationDefaults.MinPageNumber,
                            request.PageSize ?? PaginationDefaults.DefaultPageSize,
                            request.Sort),
                        cancellationToken))
            .WithName("SearchTenants")
            .WithSummary("Page through the tenant catalog")
            .Produces<PagedResponse<TenantDto>>(StatusCodes.Status200OK)
            .RequirePermission(TenancyPermissions.Search);
}

/// <summary>Query string shape for <see cref="SearchTenantsEndpoint"/>.</summary>
/// <param name="SearchTerm">Matched against the identifier and the practice name.</param>
/// <param name="IsActive">Filters by activation state.</param>
/// <param name="Plan">Filters by plan.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchTenantsRequest(
    string? SearchTerm,
    bool? IsActive,
    string? Plan,
    int? PageNumber,
    int? PageSize,
    string? Sort);
