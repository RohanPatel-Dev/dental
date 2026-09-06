using Dental.Modules.Identity.Contracts.v1.Tokens.RefreshToken;
using FluentValidation;

namespace Dental.Modules.Identity.Features.v1.Tokens.RefreshToken;

/// <summary>Validates <see cref="RefreshTokenCommand"/>.</summary>
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    /// <summary>Builds the rules.</summary>
    public RefreshTokenCommandValidator() =>
        RuleFor(c => c.RefreshToken).NotEmpty().MaximumLength(512);
}
