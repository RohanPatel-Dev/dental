using Dental.Framework.Shared.Pagination;
using Dental.Framework.Web.Auth;
using Dental.Modules.Scheduling.Contracts.Authorization;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.SearchAppointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.SearchAppointments;

/// <summary>Maps <c>GET /appointments</c>.</summary>
public static class SearchAppointmentsEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapSearchAppointmentsEndpoint(
        this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/appointments",
                (
                    [AsParameters] SearchAppointmentsRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new SearchAppointmentsQuery(
                            request.PatientId,
                            request.ProviderId,
                            request.OperatoryId,
                            request.Status,
                            request.From,
                            request.To,
                            request.PageNumber ?? PaginationDefaults.MinPageNumber,
                            request.PageSize ?? PaginationDefaults.DefaultPageSize,
                            request.Sort),
                        cancellationToken))
            .WithName("SearchAppointments")
            .WithSummary("Page through the appointment book")
            .Produces<PagedResponse<AppointmentDto>>(StatusCodes.Status200OK)
            .RequirePermission(SchedulingPermissions.Appointments.Search);
}

/// <summary>Query string shape for <see cref="SearchAppointmentsEndpoint"/>.</summary>
/// <param name="PatientId">Filters to one patient.</param>
/// <param name="ProviderId">Filters to one provider.</param>
/// <param name="OperatoryId">Filters to one chair.</param>
/// <param name="Status">Filters by lifecycle status.</param>
/// <param name="From">Earliest start time.</param>
/// <param name="To">Latest start time.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchAppointmentsRequest(
    Guid? PatientId,
    Guid? ProviderId,
    Guid? OperatoryId,
    AppointmentStatus? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int? PageNumber,
    int? PageSize,
    string? Sort);
