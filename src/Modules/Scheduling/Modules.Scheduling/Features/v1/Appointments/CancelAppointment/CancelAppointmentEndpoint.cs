using Dental.Framework.Web.Auth;
using Dental.Modules.Scheduling.Contracts.Authorization;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.CancelAppointment;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.CancelAppointment;

/// <summary>Maps <c>PUT /appointments/{id:guid}/cancellation</c>.</summary>
public static class CancelAppointmentEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapCancelAppointmentEndpoint(
        this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut(
                "/appointments/{id:guid}/cancellation",
                (
                    Guid id,
                    CancelAppointmentRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new CancelAppointmentCommand(id, request.Reason, request.IsNoShow),
                        cancellationToken))
            .WithName("CancelAppointment")
            .WithSummary("Cancel an appointment or record a non-attendance")
            .Produces<AppointmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(SchedulingPermissions.Appointments.Update);
}

/// <summary>Body shape for <see cref="CancelAppointmentEndpoint"/>.</summary>
/// <param name="Reason">Why it is being cancelled.</param>
/// <param name="IsNoShow">True when the patient simply did not attend.</param>
public sealed record CancelAppointmentRequest(string Reason, bool IsNoShow);
