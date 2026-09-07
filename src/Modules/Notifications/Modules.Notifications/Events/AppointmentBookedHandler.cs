using Dental.Framework.Eventing.Abstractions;
using Dental.Modules.Notifications.Contracts.Dtos;
using Dental.Modules.Notifications.Data;
using Dental.Modules.Notifications.Services;
using Dental.Modules.Scheduling.Contracts.Events;
using Microsoft.Extensions.Options;

namespace Dental.Modules.Notifications.Events;

/// <summary>
/// Queues a confirmation now and a reminder for shortly before the appointment.
/// </summary>
/// <param name="context">The notifications context.</param>
/// <param name="queue">Queues messages, applying the consent checks.</param>
/// <param name="options">Notification configuration.</param>
public sealed class AppointmentBookedHandler(
    NotificationsDbContext context,
    NotificationQueue queue,
    IOptions<NotificationOptions> options)
    : IIntegrationEventHandler<AppointmentBookedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        AppointmentBookedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        string tenantId = integrationEvent.TenantId ?? string.Empty;

        await queue.EnqueueAsync(
                integrationEvent.PatientId,
                NotificationKind.AppointmentBooked,
                integrationEvent.AppointmentId,
                integrationEvent.OccurredOnUtc,
                (patient, composer) => composer.ComposeBooked(patient, integrationEvent.StartsAt),
                tenantId,
                cancellationToken)
            .ConfigureAwait(false);

        DateTimeOffset remindAt =
            integrationEvent.StartsAt.AddHours(-options.Value.ReminderLeadHours);

        // A reminder for an appointment booked inside the lead window would be sent immediately,
        // right after the confirmation, so it is simply skipped.
        if (remindAt > integrationEvent.OccurredOnUtc)
        {
            await queue.EnqueueAsync(
                    integrationEvent.PatientId,
                    NotificationKind.AppointmentReminder,
                    integrationEvent.AppointmentId,
                    remindAt,
                    (patient, composer) => composer.ComposeReminder(patient, integrationEvent.StartsAt),
                    tenantId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
