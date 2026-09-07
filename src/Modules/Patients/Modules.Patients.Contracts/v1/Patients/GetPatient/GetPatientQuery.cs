using Dental.Modules.Patients.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Patients.Contracts.v1.Patients.GetPatient;

/// <summary>Reads one patient record.</summary>
/// <param name="PatientId">Patient identifier.</param>
public sealed record GetPatientQuery(Guid PatientId) : IQuery<PatientDto>;
