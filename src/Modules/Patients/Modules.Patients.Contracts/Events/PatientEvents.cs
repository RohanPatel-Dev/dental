using Dental.Framework.Eventing.Abstractions;

namespace Dental.Modules.Patients.Contracts.Events;

/// <summary>
/// Raised when a patient is registered.
/// </summary>
/// <remarks>
/// Carries the contact details rather than just an identifier, because a consumer running in a
/// SEPARATE PROCESS cannot call back into <c>IPatientService</c>. An extracted host keeps its own
/// projection of what it needs, fed by this event and by
/// <see cref="PatientContactChangedIntegrationEvent"/>.
/// </remarks>
/// <param name="PatientId">The new patient.</param>
/// <param name="ChartNumber">Practice-visible chart number.</param>
/// <param name="FullName">Display name.</param>
/// <param name="Email">Contact address.</param>
/// <param name="PhoneNumber">Contact number.</param>
/// <param name="HasReminderConsent">Whether appointment reminders are permitted.</param>
public sealed record PatientRegisteredIntegrationEvent(
    Guid PatientId,
    string ChartNumber,
    string FullName,
    string? Email,
    string? PhoneNumber,
    bool HasReminderConsent) : IntegrationEvent;

/// <summary>
/// Raised when a patient's name or contact details change, so downstream projections stay current.
/// </summary>
/// <param name="PatientId">The patient.</param>
/// <param name="FullName">Display name.</param>
/// <param name="Email">Contact address.</param>
/// <param name="PhoneNumber">Contact number.</param>
public sealed record PatientContactChangedIntegrationEvent(
    Guid PatientId,
    string FullName,
    string? Email,
    string? PhoneNumber) : IntegrationEvent;

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
