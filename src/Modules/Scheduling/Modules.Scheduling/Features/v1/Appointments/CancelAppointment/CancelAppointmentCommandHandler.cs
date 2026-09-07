using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Framework.Web.Realtime;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.Events;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.CancelAppointment;
using Dental.Modules.Scheduling.Data;
using Dental.Modules.Scheduling.Domain;
using Dental.Modules.Scheduling.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.CancelAppointment;

/// <summary>Cancels an appointment and announces it so pending reminders are withdrawn.</summary>
/// <param name="context">The scheduling context.</param>
/// <param name="appointments">Projects the result.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="realtime">Pushes the change to open day views.</param>
public sealed class CancelAppointmentCommandHandler(
    SchedulingDbContext context,
    AppointmentService appointments,
    IOutboxStore outbox,
    IRealtimeNotifier realtime) : ICommandHandler<CancelAppointmentCommand, AppointmentDto>
{
    /// <inheritdoc />
    public async ValueTask<AppointmentDto> Handle(
        CancelAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Appointment appointment = await context.Appointments
            .FirstOrDefaultAsync(a => a.Id == command.AppointmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Appointment", command.AppointmentId);

        appointment.Cancel(command.Reason, command.IsNoShow);

        await outbox.AddAsync(
                new AppointmentCancelledIntegrationEvent(
                    appointment.Id,
                    appointment.PatientId,
                    command.Reason)
                {
                    TenantId = appointment.TenantId,
                    Source = nameof(Scheduling),
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlyList<AppointmentDto> hydrated = await appointments
            .HydrateAsync([appointment], cancellationToken)
            .ConfigureAwait(false);

        await realtime
            .SendToTenantAsync(
                appointment.TenantId,
                RealtimeEvents.AppointmentCancelled,
                hydrated[0],
                cancellationToken)
            .ConfigureAwait(false);

        return hydrated[0];
    }
}
