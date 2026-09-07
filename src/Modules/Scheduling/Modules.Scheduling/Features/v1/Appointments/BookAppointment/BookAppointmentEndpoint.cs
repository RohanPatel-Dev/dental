using Dental.Framework.Web.Auth;
using Dental.Framework.Web.Middleware;
using Dental.Modules.Scheduling.Contracts.Authorization;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.BookAppointment;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.BookAppointment;

/// <summary>Maps <c>POST /appointments</c>.</summary>
public static class BookAppointmentEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapBookAppointmentEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/appointments",
                (
                    BookAppointmentCommand command,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(command, cancellationToken))
            .WithName("BookAppointment")
            .WithSummary("Book an appointment")
            .Produces<AppointmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequirePermission(SchedulingPermissions.Appointments.Create)
            .WithIdempotency();
}
