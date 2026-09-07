using Dental.Modules.Patients.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Patients.Contracts.v1.Patients.UpdateConsent;

/// <summary>Records a change to a patient's contact consent.</summary>
/// <param name="PatientId">Patient identifier.</param>
/// <param name="HasMarketingConsent">Whether marketing contact is permitted.</param>
/// <param name="HasReminderConsent">Whether appointment reminders are permitted.</param>
public sealed record UpdateConsentCommand(
    Guid PatientId,
    bool HasMarketingConsent,
    bool HasReminderConsent) : ICommand<PatientDto>;
