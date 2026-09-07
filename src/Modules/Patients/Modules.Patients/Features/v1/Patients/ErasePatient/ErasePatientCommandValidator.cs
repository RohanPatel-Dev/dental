using Dental.Modules.Patients.Contracts.v1.Patients.ErasePatient;
using FluentValidation;

namespace Dental.Modules.Patients.Features.v1.Patients.ErasePatient;

/// <summary>Validates <see cref="ErasePatientCommand"/>.</summary>
public sealed class ErasePatientCommandValidator : AbstractValidator<ErasePatientCommand>
{
    /// <summary>Builds the rules.</summary>
    public ErasePatientCommandValidator()
    {
        RuleFor(c => c.PatientId).NotEmpty();

        // Erasure is irreversible and legally significant; the reason belongs in the audit trail.
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(512);
    }
}
