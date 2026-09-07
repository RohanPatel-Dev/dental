using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Domain;

namespace Dental.Modules.Patients.Features.v1.Patients;

/// <summary>Projects <see cref="Patient"/> onto the contract DTO.</summary>
internal static class PatientMapper
{
    /// <summary>Maps one patient.</summary>
    /// <param name="patient">The entity.</param>
    /// <returns>The DTO.</returns>
    public static PatientDto ToDto(Patient patient) =>
        new(
            patient.Id,
            patient.ChartNumber,
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.Sex,
            patient.Email,
            patient.PhoneNumber,
            patient.Status,
            patient.PreferredProviderId,
            patient.Allergies,
            patient.HasMarketingConsent,
            patient.HasReminderConsent,
            patient.CreatedAt);
}
