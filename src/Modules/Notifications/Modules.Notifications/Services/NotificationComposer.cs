using System.Globalization;
using Dental.Modules.Notifications.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.Dtos;
using Microsoft.Extensions.Options;

namespace Dental.Modules.Notifications.Services;

/// <summary>Renders the subject and body of each kind of message.</summary>
/// <param name="options">Notification configuration, including the practice name.</param>
public sealed class NotificationComposer(IOptions<NotificationOptions> options)
{
    private readonly NotificationOptions _options = options.Value;

    /// <summary>Renders an appointment confirmation.</summary>
    /// <param name="patient">Recipient.</param>
    /// <param name="startsAt">When the appointment starts, in UTC.</param>
    /// <returns>The subject and body.</returns>
    public (string Subject, string Body) ComposeBooked(PatientSummaryDto patient, DateTimeOffset startsAt) =>
        (
            "Your appointment is booked",
            Wrap(patient, $"Your appointment is booked for {Format(startsAt)}.")
        );

    /// <summary>Renders an appointment reminder.</summary>
    /// <param name="patient">Recipient.</param>
    /// <param name="startsAt">When the appointment starts, in UTC.</param>
    /// <returns>The subject and body.</returns>
    public (string Subject, string Body) ComposeReminder(
        PatientSummaryDto patient,
        DateTimeOffset startsAt) =>
        (
            "A reminder about your appointment",
            Wrap(patient, $"This is a reminder that your appointment is on {Format(startsAt)}.")
        );

    /// <summary>Renders a cancellation notice.</summary>
    /// <param name="patient">Recipient.</param>
    /// <param name="reason">Why it was cancelled.</param>
    /// <returns>The subject and body.</returns>
    public (string Subject, string Body) ComposeCancelled(PatientSummaryDto patient, string reason) =>
        (
            "Your appointment has been cancelled",
            Wrap(patient, $"Your appointment has been cancelled. Reason: {reason}.")
        );

    /// <summary>Renders an invoice notice.</summary>
    /// <param name="patient">Recipient.</param>
    /// <param name="number">Invoice number.</param>
    /// <param name="total">Amount due.</param>
    /// <param name="currency">ISO 4217 currency.</param>
    /// <returns>The subject and body.</returns>
    public (string Subject, string Body) ComposeInvoice(
        PatientSummaryDto patient,
        string number,
        decimal total,
        string currency) =>
        (
            $"Invoice {number}",
            Wrap(
                patient,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Invoice {number} for {total:0.00} {currency} is now due."))
        );

    /// <summary>Picks the kind's rendering.</summary>
    /// <param name="kind">What the message is about.</param>
    /// <returns>A human readable label, used in logs and the admin console.</returns>
    public static string Describe(NotificationKind kind) => kind switch
    {
        NotificationKind.AppointmentBooked => "appointment confirmation",
        NotificationKind.AppointmentReminder => "appointment reminder",
        NotificationKind.AppointmentCancelled => "cancellation notice",
        NotificationKind.InvoiceIssued => "invoice notice",
        _ => "notification",
    };

    private string Wrap(PatientSummaryDto patient, string message) =>
        $"""
         <p>Dear {patient.FullName},</p>
         <p>{message}</p>
         <p>Kind regards,<br/>{_options.PracticeName}</p>
         """;

    private static string Format(DateTimeOffset instant) =>
        instant.ToString("dddd d MMMM yyyy 'at' HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
