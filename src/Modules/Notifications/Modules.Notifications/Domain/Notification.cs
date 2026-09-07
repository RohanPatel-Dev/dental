using Dental.Framework.Core.Domain;
using Dental.Modules.Notifications.Contracts.Dtos;

namespace Dental.Modules.Notifications.Domain;

/// <summary>One outbound message, queued and then sent by a background job.</summary>
public sealed class Notification : BaseEntity, IAuditableEntity
{
    /// <summary>Recipient.</summary>
    public Guid PatientId { get; set; }

    /// <summary>Appointment or invoice the message is about.</summary>
    public Guid? SubjectId { get; set; }

    /// <summary>What it is about.</summary>
    public NotificationKind Kind { get; set; }

    /// <summary>Where it is in its lifecycle.</summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>When it should be sent.</summary>
    public DateTimeOffset ScheduledFor { get; set; }

    /// <summary>When it was handed to the mail server.</summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>Rendered subject line.</summary>
    public string Subject { get; set; } = default!;

    /// <summary>Rendered body.</summary>
    public string Body { get; set; } = default!;

    /// <summary>
    /// Destination address, captured when the notification was queued.
    /// </summary>
    /// <remarks>
    /// Snapshotted deliberately: a reminder queued three weeks ago should go to the address the
    /// patient had at the time, and re-resolving it at send time would leak a later correction into
    /// an older message.
    /// </remarks>
    public string Recipient { get; set; } = default!;

    /// <summary>Delivery attempts made so far.</summary>
    public int Attempts { get; set; }

    /// <summary>Why it failed, when it did.</summary>
    public string? Error { get; set; }

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }

    /// <summary>True when the message is still eligible to be sent.</summary>
    /// <param name="now">Current time.</param>
    /// <returns><see langword="true"/> when it is due and still pending.</returns>
    public bool IsDue(DateTimeOffset now) =>
        Status == NotificationStatus.Pending && ScheduledFor <= now;

    /// <summary>Marks the message sent.</summary>
    /// <param name="sentAt">When it was handed to the mail server.</param>
    public void MarkSent(DateTimeOffset sentAt)
    {
        Status = NotificationStatus.Sent;
        SentAt = sentAt;
        Error = null;
    }

    /// <summary>Records a failed attempt, failing the message once the retries run out.</summary>
    /// <param name="error">What went wrong.</param>
    /// <param name="maxAttempts">Attempts allowed before giving up.</param>
    public void MarkFailed(string error, int maxAttempts)
    {
        Attempts++;
        Error = error;

        if (Attempts >= maxAttempts)
        {
            Status = NotificationStatus.Failed;
        }
    }

    /// <summary>Withdraws a pending message that should no longer be sent.</summary>
    /// <param name="status">Why it is being withdrawn.</param>
    public void Withdraw(NotificationStatus status)
    {
        if (Status == NotificationStatus.Pending)
        {
            Status = status;
        }
    }

    /// <summary>Removes the recipient address and rendered content when a patient is erased.</summary>
    public void Erase()
    {
        Recipient = string.Empty;
        Body = string.Empty;
        Subject = "Erased";
    }
}
