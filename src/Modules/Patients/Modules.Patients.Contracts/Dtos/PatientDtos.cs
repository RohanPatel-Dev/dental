namespace Dental.Modules.Patients.Contracts.Dtos;

/// <summary>Biological sex recorded for clinical purposes.</summary>
public enum PatientSex
{
    /// <summary>Not recorded.</summary>
    Unknown = 0,

    /// <summary>Female.</summary>
    Female = 1,

    /// <summary>Male.</summary>
    Male = 2,

    /// <summary>Recorded as other.</summary>
    Other = 3,
}

/// <summary>Why a patient record is no longer active.</summary>
public enum PatientStatus
{
    /// <summary>Currently under the practice's care.</summary>
    Active = 0,

    /// <summary>No longer attending, record retained.</summary>
    Inactive = 1,

    /// <summary>Transferred to another practice.</summary>
    Transferred = 2,

    /// <summary>Deceased.</summary>
    Deceased = 3,
}

/// <summary>A patient as other modules and the SPAs see them.</summary>
/// <param name="Id">Patient identifier.</param>
/// <param name="ChartNumber">Practice-visible chart number.</param>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="DateOfBirth">Date of birth.</param>
/// <param name="Sex">Recorded sex.</param>
/// <param name="Email">Contact address.</param>
/// <param name="PhoneNumber">Contact number.</param>
/// <param name="Status">Record status.</param>
/// <param name="PreferredProviderId">Provider the patient normally sees.</param>
/// <param name="Allergies">Recorded allergies.</param>
/// <param name="HasMarketingConsent">Whether the patient agreed to marketing contact.</param>
/// <param name="HasReminderConsent">Whether the patient agreed to appointment reminders.</param>
/// <param name="CreatedAt">When the record was created.</param>
public sealed record PatientDto(
    Guid Id,
    string ChartNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    PatientSex Sex,
    string? Email,
    string? PhoneNumber,
    PatientStatus Status,
    Guid? PreferredProviderId,
    IReadOnlyList<string> Allergies,
    bool HasMarketingConsent,
    bool HasReminderConsent,
    DateTimeOffset CreatedAt)
{
    /// <summary>Display name.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <summary>The minimal patient facts other modules need, without the clinical detail.</summary>
/// <param name="Id">Patient identifier.</param>
/// <param name="ChartNumber">Practice-visible chart number.</param>
/// <param name="FullName">Display name.</param>
/// <param name="Email">Contact address.</param>
/// <param name="PhoneNumber">Contact number.</param>
/// <param name="HasReminderConsent">Whether the patient agreed to appointment reminders.</param>
public sealed record PatientSummaryDto(
    Guid Id,
    string ChartNumber,
    string FullName,
    string? Email,
    string? PhoneNumber,
    bool HasReminderConsent);
