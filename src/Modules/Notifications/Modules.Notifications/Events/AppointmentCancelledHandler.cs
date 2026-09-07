using Dental.Framework.Eventing.Abstractions;
using Dental.Modules.Notifications.Contracts.Dtos;
using Dental.Modules.Notifications.Data;
using Dental.Modules.Notifications.Domain;
using Dental.Modules.Notifications.Services;
using Dental.Modules.Scheduling.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Notifications.Events;

/// <summary>
/// Withdraws the pending reminder for a cancelled appointment and queues a cancellation notice.
/// </summary>
/// <remarks>
/// Withdrawing first is the point: a patient who is told their appointment is cancelled and then
/// reminded of it the next morning has been failed by the system, not merely inconvenienced.
/// </remarks>
/// <param name="context">The notifications context.</param>
/// <param name="queue">Queues messages, applying the consent checks.</param>
/// <param name="logger">Logger.</param>
public sealed class AppointmentCancelledHandler(
    NotificationsDbContext context,
    NotificationQueue queue,
    ILogger<AppointmentCancelledHandler> logger)
    : IIntegrationEventHandler<AppointmentCancelledIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        AppointmentCancelledIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        List<Notification> pending = await context.Notifications
            .Where(n => n.SubjectId == integrationEvent.AppointmentId
                        && n.Status == NotificationStatus.Pending)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (Notification notification in pending)
        {
            notification.Withdraw(NotificationStatus.Cancelled);
        }

        await queue.EnqueueAsync(
                integrationEvent.PatientId,
                NotificationKind.AppointmentCancelled,
                integrationEvent.AppointmentId,
                integrationEvent.OccurredOnUtc,
                (patient, composer) => composer.ComposeCancelled(patient, integrationEvent.Reason),
                integrationEvent.TenantId ?? string.Empty,
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Withdrew {Count} pending notification(s) for cancelled appointment {AppointmentId}.",
            pending.Count,
            integrationEvent.AppointmentId);
    }
}
