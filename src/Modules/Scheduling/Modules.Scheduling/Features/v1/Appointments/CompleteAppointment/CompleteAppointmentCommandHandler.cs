using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Framework.Web.Realtime;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.Events;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.CompleteAppointment;
using Dental.Modules.Scheduling.Data;
using Dental.Modules.Scheduling.Domain;
using Dental.Modules.Scheduling.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.CompleteAppointment;

/// <summary>
/// Marks treatment complete, which is the trigger the Clinical and Billing modules listen for.
/// </summary>
/// <param name="context">The scheduling context.</param>
/// <param name="appointments">Projects the result.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="realtime">Pushes the change to open day views.</param>
/// <param name="timeProvider">Clock.</param>
public sealed class CompleteAppointmentCommandHandler(
    SchedulingDbContext context,
    AppointmentService appointments,
    IOutboxStore outbox,
    IRealtimeNotifier realtime,
    TimeProvider timeProvider) : ICommandHandler<CompleteAppointmentCommand, AppointmentDto>
{
    /// <inheritdoc />
    public async ValueTask<AppointmentDto> Handle(
        CompleteAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Appointment appointment = await context.Appointments
            .FirstOrDefaultAsync(a => a.Id == command.AppointmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Appointment", command.AppointmentId);

        DateTimeOffset now = timeProvider.GetUtcNow();
        appointment.Complete(now);

        await outbox.AddAsync(
                new AppointmentCompletedIntegrationEvent(
                    appointment.Id,
                    appointment.PatientId,
                    appointment.ProviderId,
                    now)
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
                RealtimeEvents.AppointmentCompleted,
                hydrated[0],
                cancellationToken)
            .ConfigureAwait(false);

        return hydrated[0];
    }
}
