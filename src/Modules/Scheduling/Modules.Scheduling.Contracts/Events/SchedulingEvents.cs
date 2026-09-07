using Dental.Framework.Eventing.Abstractions;
using Dental.Modules.Scheduling.Contracts.Dtos;

namespace Dental.Modules.Scheduling.Contracts.Events;

/// <summary>Raised when an appointment is booked.</summary>
/// <param name="AppointmentId">The new appointment.</param>
/// <param name="PatientId">Patient it is for.</param>
/// <param name="ProviderId">Provider delivering it.</param>
/// <param name="StartsAt">Start time, in UTC.</param>
/// <param name="Kind">What the appointment is for.</param>
public sealed record AppointmentBookedIntegrationEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid ProviderId,
    DateTimeOffset StartsAt,
    AppointmentKind Kind) : IntegrationEvent;

/// <summary>Raised when an appointment is cancelled, so reminders are withdrawn.</summary>
/// <param name="AppointmentId">The cancelled appointment.</param>
/// <param name="PatientId">Patient it was for.</param>
/// <param name="Reason">Why it was cancelled.</param>
public sealed record AppointmentCancelledIntegrationEvent(
    Guid AppointmentId,
    Guid PatientId,
    string Reason) : IntegrationEvent;

/// <summary>
/// Raised when treatment finishes, so the Clinical module can open a chart entry and Billing can
/// raise the charges.
/// </summary>
/// <param name="AppointmentId">The completed appointment.</param>
/// <param name="PatientId">Patient it was for.</param>
/// <param name="ProviderId">Provider who delivered it.</param>
/// <param name="CompletedAt">When it was marked complete.</param>
public sealed record AppointmentCompletedIntegrationEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid ProviderId,
    DateTimeOffset CompletedAt) : IntegrationEvent;
