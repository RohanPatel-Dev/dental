namespace Dental.Modules.Notifications.Contracts.Dtos;

/// <summary>What a notification is about.</summary>
public enum NotificationKind
{
    /// <summary>Confirmation that an appointment was booked.</summary>
    AppointmentBooked = 0,

    /// <summary>Reminder that an appointment is coming up.</summary>
    AppointmentReminder = 1,

    /// <summary>Notice that an appointment was cancelled.</summary>
    AppointmentCancelled = 2,

    /// <summary>Notice that an invoice is due.</summary>
    InvoiceIssued = 3,
}

/// <summary>Where a notification is in its lifecycle.</summary>
public enum NotificationStatus
{
    /// <summary>Queued, not yet attempted.</summary>
    Pending = 0,

    /// <summary>Handed to the mail server.</summary>
    Sent = 1,

    /// <summary>Delivery failed after every retry.</summary>
    Failed = 2,

    /// <summary>Not sent because the patient withdrew consent.</summary>
    SuppressedByConsent = 3,

    /// <summary>Not sent because the underlying appointment was cancelled.</summary>
    Cancelled = 4,
}

/// <summary>One outbound notification.</summary>
/// <param name="Id">Notification identifier.</param>
/// <param name="PatientId">Recipient.</param>
/// <param name="Kind">What it is about.</param>
/// <param name="Status">Where it is in its lifecycle.</param>
/// <param name="ScheduledFor">When it should be sent.</param>
/// <param name="SentAt">When it was handed to the mail server.</param>
/// <param name="Subject">Rendered subject line.</param>
/// <param name="Error">Why it failed, when it did.</param>
public sealed record NotificationDto(
    Guid Id,
    Guid PatientId,
    NotificationKind Kind,
    NotificationStatus Status,
    DateTimeOffset ScheduledFor,
    DateTimeOffset? SentAt,
    string Subject,
    string? Error);
