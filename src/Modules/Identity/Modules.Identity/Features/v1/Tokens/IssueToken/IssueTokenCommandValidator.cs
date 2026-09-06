using Dental.Framework.Shared.Identity;
using Dental.Modules.Identity.Contracts.v1.Tokens.IssueToken;
using FluentValidation;

namespace Dental.Modules.Identity.Features.v1.Tokens.IssueToken;

/// <summary>Validates <see cref="IssueTokenCommand"/>.</summary>
public sealed class IssueTokenCommandValidator : AbstractValidator<IssueTokenCommand>
{
    /// <summary>Builds the rules.</summary>
    public IssueTokenCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.Password).NotEmpty().MaximumLength(256);

        RuleFor(c => c.App)
            .NotEmpty()
            .Must(app => app == DentalApps.Admin || app == DentalApps.Dashboard)
            .WithMessage($"App must be '{DentalApps.Admin}' or '{DentalApps.Dashboard}'.");
    }
}
