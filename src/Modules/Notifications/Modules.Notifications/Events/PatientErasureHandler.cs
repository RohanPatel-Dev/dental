using Dental.Framework.Eventing.Abstractions;
using Dental.Modules.Notifications.Data;
using Dental.Modules.Notifications.Domain;
using Dental.Modules.Notifications.Services;
using Dental.Modules.Patients.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Notifications.Events;

/// <summary>
/// Withdraws queued messages and strips the recipient address and rendered content when a patient's
/// record is erased.
/// </summary>
/// <remarks>
/// The rendered body is a copy of the patient's name and appointment details, so it is erased
/// outright. The rows stay only as a record that a contact was attempted.
/// </remarks>
/// <param name="context">The notifications context.</param>
/// <param name="projection">The local patient projection.</param>
/// <param name="logger">Logger.</param>
public sealed class PatientErasureHandler(
    NotificationsDbContext context,
    PatientContactProjection projection,
    ILogger<PatientErasureHandler> logger)
    : IIntegrationEventHandler<PatientErasureRequestedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        PatientErasureRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        List<Notification> notifications = await context.Notifications
            .Where(n => n.PatientId == integrationEvent.PatientId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (Notification notification in notifications)
        {
            notification.Withdraw(Contracts.Dtos.NotificationStatus.Cancelled);
            notification.Erase();
        }

        // The local projection is a copy of the patient's contact details, so it is erased too.
        PatientContact? contact = await projection
            .FindAsync(integrationEvent.PatientId, cancellationToken)
            .ConfigureAwait(false);

        contact?.Erase();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Erased {Count} notification(s) for patient {PatientId}.",
            notifications.Count,
            integrationEvent.PatientId);
    }
}
