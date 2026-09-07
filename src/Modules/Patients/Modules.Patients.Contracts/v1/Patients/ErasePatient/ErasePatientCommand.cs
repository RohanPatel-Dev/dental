using Mediator;

namespace Dental.Modules.Patients.Contracts.v1.Patients.ErasePatient;

/// <summary>
/// Erases a patient's personal data and announces the erasure so every other module drops its copy.
/// </summary>
/// <param name="PatientId">Patient identifier.</param>
/// <param name="Reason">Why the erasure was requested. Recorded in the audit trail.</param>
public sealed record ErasePatientCommand(Guid PatientId, string Reason) : ICommand<ErasePatientResponse>;

/// <summary>Outcome of an erasure request.</summary>
/// <param name="PatientId">The erased patient.</param>
/// <param name="ErasedAt">When the erasure was applied in this module.</param>
public sealed record ErasePatientResponse(Guid PatientId, DateTimeOffset ErasedAt);
