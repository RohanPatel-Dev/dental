using Dental.Modules.Identity.Contracts.v1.Users.CreateUser;
using FluentValidation;

namespace Dental.Modules.Identity.Features.v1.Users.CreateUser;

/// <summary>Validates <see cref="CreateUserCommand"/>.</summary>
public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>Builds the rules.</summary>
    public CreateUserCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(128);
        RuleFor(c => c.PhoneNumber).MaximumLength(32);

        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(12)
            .WithMessage("Password must be at least 12 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain an upper case letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lower case letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");

        RuleFor(c => c.Roles).NotNull();
        RuleForEach(c => c.Roles).NotEmpty().MaximumLength(128);
    }
}
