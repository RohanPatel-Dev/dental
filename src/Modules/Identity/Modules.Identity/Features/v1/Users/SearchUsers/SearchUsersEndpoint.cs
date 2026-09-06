using Dental.Framework.Shared.Pagination;
using Dental.Framework.Web.Auth;
using Dental.Modules.Identity.Contracts.Authorization;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Users.SearchUsers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Identity.Features.v1.Users.SearchUsers;

/// <summary>Maps <c>GET /users</c>.</summary>
public static class SearchUsersEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapSearchUsersEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/users",
                (
                    [AsParameters] SearchUsersRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new SearchUsersQuery(
                            request.SearchTerm,
                            request.Role,
                            request.IsActive,
                            request.PageNumber ?? PaginationDefaults.MinPageNumber,
                            request.PageSize ?? PaginationDefaults.DefaultPageSize,
                            request.Sort),
                        cancellationToken))
            .WithName("SearchUsers")
            .WithSummary("Page through the users of the current practice")
            .Produces<PagedResponse<UserDto>>(StatusCodes.Status200OK)
            .RequirePermission(IdentityPermissions.Users.Search);
}

/// <summary>Query string shape for <see cref="SearchUsersEndpoint"/>.</summary>
/// <param name="SearchTerm">Matched against email and name.</param>
/// <param name="Role">Filters by role name.</param>
/// <param name="IsActive">Filters by activation state.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchUsersRequest(
    string? SearchTerm,
    string? Role,
    bool? IsActive,
    int? PageNumber,
    int? PageSize,
    string? Sort);
