using Dental.Modules.Notifications.Contracts.Dtos;
using Dental.Modules.Notifications.Data;
using Dental.Modules.Notifications.Domain;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Notifications.Services;

/// <summary>Queues a message after checking consent and that a contact address exists.</summary>
/// <param name="context">The notifications context.</param>
/// <param name="projection">
/// The module's own patient projection. Deliberately NOT <c>IPatientService</c>: this module runs
/// in its own process, where that service does not exist.
/// </param>
/// <param name="composer">Renders the message.</param>
/// <param name="logger">Logger.</param>
public sealed class NotificationQueue(
    NotificationsDbContext context,
    PatientContactProjection projection,
    NotificationComposer composer,
    ILogger<NotificationQueue> logger)
{
    /// <summary>
    /// Queues a message, or declines to when the patient has not consented or cannot be reached.
    /// </summary>
    /// <param name="patientId">Recipient.</param>
    /// <param name="kind">What the message is about.</param>
    /// <param name="subjectId">Appointment or invoice it concerns.</param>
    /// <param name="scheduledFor">When it should be sent.</param>
    /// <param name="render">Renders the subject and body from the resolved patient.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The queued notification, or null when nothing was queued.</returns>
    /// <remarks>
    /// Consent is checked HERE, at queue time, and again at send time. Checking only once would
    /// either send to a patient who withdrew consent after booking, or drop a message for one who
    /// granted it afterwards.
    /// </remarks>
    public async Task<Notification?> EnqueueAsync(
        Guid patientId,
        NotificationKind kind,
        Guid? subjectId,
        DateTimeOffset scheduledFor,
        Func<PatientContact, NotificationComposer, (string Subject, string Body)> render,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(render);

        PatientContact? patient = await projection.FindAsync(patientId, cancellationToken)
            .ConfigureAwait(false);

        if (patient is null)
        {
            // The projection has not caught up, or this host was deployed without a backfill.
            // Declining to send is the safe failure: the alternative is mailing an address the
            // module has not been told about.
            logger.LogWarning(
                "Not queueing a {Kind}: patient {PatientId} is not in the local projection.",
                NotificationComposer.Describe(kind),
                patientId);
            return null;
        }

        if (!patient.IsContactable)
        {
            logger.LogInformation(
                "Not queueing a {Kind} for patient {PatientId}: no consent or no address on file.",
                NotificationComposer.Describe(kind),
                patientId);
            return null;
        }

        (string subject, string body) = render(patient, composer);

        Notification notification = new()
        {
            PatientId = patientId,
            SubjectId = subjectId,
            Kind = kind,
            Status = NotificationStatus.Pending,
            ScheduledFor = scheduledFor,
            Subject = subject,
            Body = body,
            Recipient = patient.Email!,
            TenantId = tenantId,
        };

        context.Notifications.Add(notification);
        return notification;
    }
}
