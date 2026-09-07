using Dental.Framework.Web.Auth;
using Dental.Framework.Web.Middleware;
using Dental.Modules.Scheduling.Contracts.Authorization;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.CompleteAppointment;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.CompleteAppointment;

/// <summary>Maps <c>POST /appointments/{id:guid}/completion</c>.</summary>
public static class CompleteAppointmentEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapCompleteAppointmentEndpoint(
        this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/appointments/{id:guid}/completion",
                (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(new CompleteAppointmentCommand(id), cancellationToken))
            .WithName("CompleteAppointment")
            .WithSummary("Mark an appointment complete")
            .Produces<AppointmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequirePermission(SchedulingPermissions.Appointments.Update)
            .WithIdempotency();
}
