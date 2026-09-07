using Dental.Framework.Core.ValueObjects;
using Dental.Modules.Patients.Contracts.v1.Patients.RegisterPatient;
using FluentValidation;

namespace Dental.Modules.Patients.Features.v1.Patients.RegisterPatient;

/// <summary>Validates <see cref="RegisterPatientCommand"/>.</summary>
public sealed class RegisterPatientCommandValidator : AbstractValidator<RegisterPatientCommand>
{
    /// <summary>Builds the rules.</summary>
    public RegisterPatientCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(128);

        RuleFor(c => c.DateOfBirth)
            .NotEqual(default(DateOnly))
            .Must(dob => dob <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.")
            .Must(dob => dob >= new DateOnly(1900, 1, 1))
            .WithMessage("Date of birth must be after 1900.");

        RuleFor(c => c.Sex).IsInEnum();
        RuleFor(c => c.Email).EmailAddress().MaximumLength(256).When(c => !string.IsNullOrEmpty(c.Email));

        RuleFor(c => c.PhoneNumber)
            .Must(phone => PhoneNumber.TryParse(phone, out _))
            .When(c => !string.IsNullOrEmpty(c.PhoneNumber))
            .WithMessage("PhoneNumber is not a valid phone number.");

        // A practice must be able to reach the patient somehow; requiring at least one channel here
        // is cheaper than discovering it when a reminder silently fails to send.
        RuleFor(c => c)
            .Must(c => !string.IsNullOrWhiteSpace(c.Email) || !string.IsNullOrWhiteSpace(c.PhoneNumber))
            .WithName("Contact")
            .WithMessage("Either an email address or a phone number is required.");

        RuleFor(c => c.Allergies).NotNull();
        RuleForEach(c => c.Allergies).NotEmpty().MaximumLength(128);
    }
}
