using Dental.Framework.Web.Auth;
using Dental.Modules.Scheduling.Contracts.Authorization;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.GetAppointment;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.GetAppointment;

/// <summary>Maps <c>GET /appointments/{id:guid}</c>.</summary>
public static class GetAppointmentEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapGetAppointmentEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/appointments/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(new GetAppointmentQuery(id), cancellationToken))
            .WithName("GetAppointment")
            .WithSummary("Read one appointment")
            .Produces<AppointmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(SchedulingPermissions.Appointments.View);
}
