using Dental.Modules.Patients.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Patients.Contracts.v1.Patients.UpdatePatient;

/// <summary>Amends a patient record.</summary>
/// <param name="PatientId">Patient identifier.</param>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="Email">Contact address.</param>
/// <param name="PhoneNumber">Contact number.</param>
/// <param name="Status">Record status.</param>
/// <param name="PreferredProviderId">Provider the patient normally sees.</param>
/// <param name="Allergies">Recorded allergies.</param>
public sealed record UpdatePatientCommand(
    Guid PatientId,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    PatientStatus Status,
    Guid? PreferredProviderId,
    IReadOnlyList<string> Allergies) : ICommand<PatientDto>;
