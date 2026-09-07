using Dental.Modules.Patients.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Patients.Contracts.v1.Patients.RegisterPatient;

/// <summary>Registers a new patient with the practice.</summary>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="DateOfBirth">Date of birth.</param>
/// <param name="Sex">Recorded sex.</param>
/// <param name="Email">Contact address.</param>
/// <param name="PhoneNumber">Contact number.</param>
/// <param name="PreferredProviderId">Provider the patient normally sees.</param>
/// <param name="Allergies">Recorded allergies.</param>
/// <param name="HasMarketingConsent">Whether the patient agreed to marketing contact.</param>
/// <param name="HasReminderConsent">Whether the patient agreed to appointment reminders.</param>
public sealed record RegisterPatientCommand(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    PatientSex Sex,
    string? Email,
    string? PhoneNumber,
    Guid? PreferredProviderId,
    IReadOnlyList<string> Allergies,
    bool HasMarketingConsent,
    bool HasReminderConsent) : ICommand<PatientDto>;
