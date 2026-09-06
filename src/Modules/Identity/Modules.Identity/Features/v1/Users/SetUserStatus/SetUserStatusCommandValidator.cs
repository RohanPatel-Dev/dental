using Dental.Modules.Identity.Contracts.v1.Users.SetUserStatus;
using FluentValidation;

namespace Dental.Modules.Identity.Features.v1.Users.SetUserStatus;

/// <summary>Validates <see cref="SetUserStatusCommand"/>.</summary>
public sealed class SetUserStatusCommandValidator : AbstractValidator<SetUserStatusCommand>
{
    /// <summary>Builds the rules.</summary>
    public SetUserStatusCommandValidator() => RuleFor(c => c.UserId).NotEmpty();
}
