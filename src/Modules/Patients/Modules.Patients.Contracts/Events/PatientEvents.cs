using Dental.Framework.Eventing.Abstractions;

namespace Dental.Modules.Patients.Contracts.Events;

/// <summary>Raised when a patient is registered.</summary>
/// <param name="PatientId">The new patient.</param>
/// <param name="ChartNumber">Practice-visible chart number.</param>
/// <param name="FullName">Display name.</param>
public sealed record PatientRegisteredIntegrationEvent(
    Guid PatientId,
    string ChartNumber,
    string FullName) : IntegrationEvent;

/// <summary>
/// Raised when a patient exercises their right to erasure, so every module deletes its own copy.
/// </summary>
/// <remarks>
/// Erasure is a fan-out: the Patients module cannot reach into Scheduling's or Billing's tables, and
/// should not try. Each module handles this event and erases what it owns.
/// </remarks>
/// <param name="PatientId">The patient to erase.</param>
/// <param name="Reason">Why the erasure was requested.</param>
public sealed record PatientErasureRequestedIntegrationEvent(Guid PatientId, string Reason)
    : IntegrationEvent;

/// <summary>Raised when a patient's contact consent changes.</summary>
/// <param name="PatientId">The patient.</param>
/// <param name="HasMarketingConsent">Whether marketing contact is now permitted.</param>
/// <param name="HasReminderConsent">Whether appointment reminders are now permitted.</param>
public sealed record PatientConsentChangedIntegrationEvent(
    Guid PatientId,
    bool HasMarketingConsent,
    bool HasReminderConsent) : IntegrationEvent;
