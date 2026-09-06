using Dental.Framework.Shared.Pagination;
using Dental.Framework.Web.Auth;
using Dental.Modules.Auditing.Contracts.Authorization;
using Dental.Modules.Auditing.Contracts.Dtos;
using Dental.Modules.Auditing.Contracts.v1.AuditTrails.SearchAuditTrails;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Auditing.Features.v1.AuditTrails.SearchAuditTrails;

/// <summary>Maps <c>GET /audit-trails</c>.</summary>
public static class SearchAuditTrailsEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapSearchAuditTrailsEndpoint(
        this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/audit-trails",
                (
                    [AsParameters] SearchAuditTrailsRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new SearchAuditTrailsQuery(
                            request.EntityName,
                            request.EntityId,
                            request.UserId,
                            request.Operation,
                            request.From,
                            request.To,
                            request.PageNumber ?? PaginationDefaults.MinPageNumber,
                            request.PageSize ?? PaginationDefaults.DefaultPageSize,
                            request.Sort),
                        cancellationToken))
            .WithName("SearchAuditTrails")
            .WithSummary("Page through the audit trail")
            .Produces<PagedResponse<AuditTrailDto>>(StatusCodes.Status200OK)
            .RequirePermission(AuditingPermissions.Search);
}

/// <summary>Query string shape for <see cref="SearchAuditTrailsEndpoint"/>.</summary>
/// <param name="EntityName">Filters by entity type.</param>
/// <param name="EntityId">Filters by the identifier of one row.</param>
/// <param name="UserId">Filters by the user that made the change.</param>
/// <param name="Operation">Filters by operation name.</param>
/// <param name="From">Earliest change to include.</param>
/// <param name="To">Latest change to include.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchAuditTrailsRequest(
    string? EntityName,
    string? EntityId,
    Guid? UserId,
    string? Operation,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int? PageNumber,
    int? PageSize,
    string? Sort);
