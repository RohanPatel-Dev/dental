using Dental.Framework.Web.Auth;
using Dental.Modules.Scheduling.Contracts.Authorization;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.RescheduleAppointment;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.RescheduleAppointment;

/// <summary>Maps <c>PUT /appointments/{id:guid}/schedule</c>.</summary>
public static class RescheduleAppointmentEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapRescheduleAppointmentEndpoint(
        this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut(
                "/appointments/{id:guid}/schedule",
                (
                    Guid id,
                    RescheduleAppointmentRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new RescheduleAppointmentCommand(
                            id,
                            request.StartsAt,
                            request.DurationMinutes,
                            request.OperatoryId),
                        cancellationToken))
            .WithName("RescheduleAppointment")
            .WithSummary("Move an appointment to a new slot")
            .Produces<AppointmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequirePermission(SchedulingPermissions.Appointments.Update);
}

/// <summary>Body shape for <see cref="RescheduleAppointmentEndpoint"/>.</summary>
/// <param name="StartsAt">New start time, in UTC.</param>
/// <param name="DurationMinutes">New duration.</param>
/// <param name="OperatoryId">New chair, when it changes.</param>
public sealed record RescheduleAppointmentRequest(
    DateTimeOffset StartsAt,
    int DurationMinutes,
    Guid? OperatoryId);
