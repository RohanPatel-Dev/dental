using Dental.Modules.Patients.Contracts.v1.Patients.UpdateConsent;
using FluentValidation;

namespace Dental.Modules.Patients.Features.v1.Patients.UpdateConsent;

/// <summary>Validates <see cref="UpdateConsentCommand"/>.</summary>
public sealed class UpdateConsentCommandValidator : AbstractValidator<UpdateConsentCommand>
{
    /// <summary>Builds the rules.</summary>
    public UpdateConsentCommandValidator() => RuleFor(c => c.PatientId).NotEmpty();
}
