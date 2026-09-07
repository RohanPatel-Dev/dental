using Dental.Framework.Core.ValueObjects;
using Dental.Modules.Patients.Contracts.v1.Patients.UpdatePatient;
using FluentValidation;

namespace Dental.Modules.Patients.Features.v1.Patients.UpdatePatient;

/// <summary>Validates <see cref="UpdatePatientCommand"/>.</summary>
public sealed class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    /// <summary>Builds the rules.</summary>
    public UpdatePatientCommandValidator()
    {
        RuleFor(c => c.PatientId).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Status).IsInEnum();
        RuleFor(c => c.Email).EmailAddress().MaximumLength(256).When(c => !string.IsNullOrEmpty(c.Email));

        RuleFor(c => c.PhoneNumber)
            .Must(phone => PhoneNumber.TryParse(phone, out _))
            .When(c => !string.IsNullOrEmpty(c.PhoneNumber))
            .WithMessage("PhoneNumber is not a valid phone number.");

        RuleFor(c => c.Allergies).NotNull();
        RuleForEach(c => c.Allergies).NotEmpty().MaximumLength(128);
    }
}
