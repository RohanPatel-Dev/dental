using System.ComponentModel.DataAnnotations;

namespace Dental.Modules.Notifications;

/// <summary>Notification configuration, bound from the <c>NotificationOptions</c> section.</summary>
public sealed class NotificationOptions
{
    /// <summary>Hours before an appointment that a reminder is sent.</summary>
    [Range(1, 336)]
    public int ReminderLeadHours { get; set; } = 24;

    /// <summary>Delivery attempts before a message is marked failed.</summary>
    [Range(1, 10)]
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Messages handed to the mail server per sweep.</summary>
    [Range(1, 1000)]
    public int BatchSize { get; set; } = 50;

    /// <summary>Cron expression for the dispatch sweep. Defaults to every fifteen minutes.</summary>
    public string DispatchCron { get; set; } = "*/15 * * * *";

    /// <summary>Practice name used in the message signature.</summary>
    public string PracticeName { get; set; } = "Your dental practice";
}
