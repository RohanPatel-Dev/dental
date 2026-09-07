using Dental.Framework.Eventing.Abstractions;
using Dental.Modules.Notifications.Contracts.Dtos;
using Dental.Modules.Notifications.Data;
using Dental.Modules.Notifications.Domain;
using Dental.Modules.Patients.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Notifications.Events;

/// <summary>
/// Withdraws every queued message for a patient who has withdrawn contact consent.
/// </summary>
/// <remarks>
/// Consent withdrawal must take effect on messages already in the queue, not merely on future ones.
/// A reminder queued last week and sent tomorrow is still a contact the patient has refused.
/// </remarks>
/// <param name="context">The notifications context.</param>
/// <param name="logger">Logger.</param>
public sealed class PatientConsentChangedHandler(
    NotificationsDbContext context,
    ILogger<PatientConsentChangedHandler> logger)
    : IIntegrationEventHandler<PatientConsentChangedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        PatientConsentChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (integrationEvent.HasReminderConsent)
        {
            return;
        }

        List<Notification> pending = await context.Notifications
            .Where(n => n.PatientId == integrationEvent.PatientId
                        && n.Status == NotificationStatus.Pending)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (Notification notification in pending)
        {
            notification.Withdraw(NotificationStatus.SuppressedByConsent);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Suppressed {Count} queued notification(s) for patient {PatientId} after consent withdrawal.",
            pending.Count,
            integrationEvent.PatientId);
    }
}
