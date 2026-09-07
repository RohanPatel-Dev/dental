using Dental.Modules.Scheduling.Contracts.v1.Providers.CreateProvider;
using FluentValidation;

namespace Dental.Modules.Scheduling.Features.v1.Providers.CreateProvider;

/// <summary>Validates <see cref="CreateProviderCommand"/>.</summary>
public sealed class CreateProviderCommandValidator : AbstractValidator<CreateProviderCommand>
{
    /// <summary>Builds the rules.</summary>
    public CreateProviderCommandValidator()
    {
        RuleFor(c => c.DisplayName).NotEmpty().MaximumLength(256);
        RuleFor(c => c.Speciality).MaximumLength(128);
    }
}
