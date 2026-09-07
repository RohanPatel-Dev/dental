using Dental.Framework.Shared.Pagination;
using Dental.Framework.Web.Auth;
using Dental.Modules.Notifications.Contracts.Authorization;
using Dental.Modules.Notifications.Contracts.Dtos;
using Dental.Modules.Notifications.Contracts.v1.Notifications.SearchNotifications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Notifications.Features.v1.Notifications.SearchNotifications;

/// <summary>Maps <c>GET /notifications</c>.</summary>
public static class SearchNotificationsEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapSearchNotificationsEndpoint(
        this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/notifications",
                (
                    [AsParameters] SearchNotificationsRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new SearchNotificationsQuery(
                            request.PatientId,
                            request.Kind,
                            request.Status,
                            request.PageNumber ?? PaginationDefaults.MinPageNumber,
                            request.PageSize ?? PaginationDefaults.DefaultPageSize,
                            request.Sort),
                        cancellationToken))
            .WithName("SearchNotifications")
            .WithSummary("Page through the outbound notification log")
            .Produces<PagedResponse<NotificationDto>>(StatusCodes.Status200OK)
            .RequirePermission(NotificationsPermissions.Search);
}

/// <summary>Query string shape for <see cref="SearchNotificationsEndpoint"/>.</summary>
/// <param name="PatientId">Filters to one patient.</param>
/// <param name="Kind">Filters by what the notification is about.</param>
/// <param name="Status">Filters by lifecycle status.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchNotificationsRequest(
    Guid? PatientId,
    NotificationKind? Kind,
    NotificationStatus? Status,
    int? PageNumber,
    int? PageSize,
    string? Sort);
