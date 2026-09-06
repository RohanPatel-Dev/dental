using Dental.Modules.Identity.Contracts.v1.Users.AssignRoles;
using FluentValidation;

namespace Dental.Modules.Identity.Features.v1.Users.AssignRoles;

/// <summary>Validates <see cref="AssignRolesCommand"/>.</summary>
public sealed class AssignRolesCommandValidator : AbstractValidator<AssignRolesCommand>
{
    /// <summary>Builds the rules.</summary>
    public AssignRolesCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Roles).NotNull();
        RuleForEach(c => c.Roles).NotEmpty().MaximumLength(128);
    }
}
