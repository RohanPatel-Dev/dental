using Dental.Framework.Core.Exceptions;
using Dental.Framework.Web.Realtime;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.RescheduleAppointment;
using Dental.Modules.Scheduling.Data;
using Dental.Modules.Scheduling.Domain;
using Dental.Modules.Scheduling.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.RescheduleAppointment;

/// <summary>Moves an appointment to a new slot.</summary>
/// <param name="context">The scheduling context.</param>
/// <param name="availability">Rejects a double booking.</param>
/// <param name="appointments">Projects the result.</param>
/// <param name="realtime">Pushes the change to open day views.</param>
public sealed class RescheduleAppointmentCommandHandler(
    SchedulingDbContext context,
    SlotAvailabilityChecker availability,
    AppointmentService appointments,
    IRealtimeNotifier realtime) : ICommandHandler<RescheduleAppointmentCommand, AppointmentDto>
{
    /// <inheritdoc />
    public async ValueTask<AppointmentDto> Handle(
        RescheduleAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Appointment appointment = await context.Appointments
            .FirstOrDefaultAsync(a => a.Id == command.AppointmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Appointment", command.AppointmentId);

        Guid operatoryId = command.OperatoryId ?? appointment.OperatoryId;
        DateTimeOffset endsAt = command.StartsAt.AddMinutes(command.DurationMinutes);

        await availability
            .EnsureFreeAsync(
                appointment.ProviderId,
                operatoryId,
                command.StartsAt,
                endsAt,
                excludingAppointmentId: appointment.Id,
                cancellationToken)
            .ConfigureAwait(false);

        appointment.Reschedule(
            command.StartsAt,
            TimeSpan.FromMinutes(command.DurationMinutes),
            command.OperatoryId);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlyList<AppointmentDto> hydrated = await appointments
            .HydrateAsync([appointment], cancellationToken)
            .ConfigureAwait(false);

        await realtime
            .SendToTenantAsync(
                appointment.TenantId,
                RealtimeEvents.AppointmentRescheduled,
                hydrated[0],
                cancellationToken)
            .ConfigureAwait(false);

        return hydrated[0];
    }
}
