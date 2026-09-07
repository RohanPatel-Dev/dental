using Dental.Framework.Mailing;
using Dental.Modules.Notifications.Contracts.Dtos;
using Dental.Modules.Notifications.Data;
using Dental.Modules.Notifications.Domain;
using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dental.Modules.Notifications.Jobs;

/// <summary>Hands due messages to the mail server, re-checking consent immediately before sending.</summary>
/// <param name="context">The notifications context.</param>
/// <param name="mailService">SMTP transport.</param>
/// <param name="patientService">Re-checks consent at send time.</param>
/// <param name="options">Notification configuration.</param>
/// <param name="timeProvider">Clock.</param>
/// <param name="logger">Logger.</param>
public sealed class NotificationDispatchJob(
    NotificationsDbContext context,
    IMailService mailService,
    IPatientService patientService,
    IOptions<NotificationOptions> options,
    TimeProvider timeProvider,
    ILogger<NotificationDispatchJob> logger)
{
    /// <summary>Sends up to one batch of due messages.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the batch has been attempted.</returns>
    /// <remarks>
    /// Hangfire's own retry is used rather than an in-process policy: it survives a restart, which
    /// an in-memory pipeline does not.
    /// </remarks>
    [AutomaticRetry(Attempts = 2)]
    [Queue("email")]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        NotificationOptions settings = options.Value;
        DateTimeOffset now = timeProvider.GetUtcNow();

        List<Notification> due = await context.Notifications
            .Where(n => n.Status == NotificationStatus.Pending && n.ScheduledFor <= now)
            .OrderBy(n => n.ScheduledFor)
            .Take(settings.BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (due.Count == 0)
        {
            return;
        }

        int sent = 0;

        foreach (Notification notification in due)
        {
            if (await SendAsync(notification, settings, now, cancellationToken).ConfigureAwait(false))
            {
                sent++;
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Dispatched {SentCount} of {DueCount} due notification(s).",
            sent,
            due.Count);
    }

    private async Task<bool> SendAsync(
        Notification notification,
        NotificationOptions settings,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Consent is re-checked here, not just at queue time: a patient may have withdrawn it in
        // the hours or days since the message was queued.
        PatientSummaryDto? patient = await patientService
            .GetSummaryAsync(notification.PatientId, cancellationToken)
            .ConfigureAwait(false);

        if (patient is null || !patient.HasReminderConsent)
        {
            notification.Withdraw(NotificationStatus.SuppressedByConsent);
            logger.LogInformation(
                "Suppressed notification {NotificationId}: consent is no longer held.",
                notification.Id);
            return false;
        }

        MailRequest request = new()
        {
            Subject = notification.Subject,
            HtmlBody = notification.Body,
        };

        request.To.Add(notification.Recipient);

        try
        {
            await mailService.SendAsync(request, cancellationToken).ConfigureAwait(false);
            notification.MarkSent(now);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            notification.MarkFailed(exception.Message, settings.MaxAttempts);

            // Logged with the notification id only - the body contains the patient's name and
            // appointment details, and must never reach the log.
            logger.LogError(
                exception,
                "Failed to send notification {NotificationId} on attempt {Attempts}.",
                notification.Id,
                notification.Attempts);

            return false;
        }
    }
}
