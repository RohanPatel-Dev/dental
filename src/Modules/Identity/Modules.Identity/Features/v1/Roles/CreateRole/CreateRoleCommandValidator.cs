using Dental.Framework.Shared.Identity;
using Dental.Modules.Identity.Contracts.v1.Roles.CreateRole;
using FluentValidation;

namespace Dental.Modules.Identity.Features.v1.Roles.CreateRole;

/// <summary>Validates <see cref="CreateRoleCommand"/>.</summary>
public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    /// <summary>Builds the rules.</summary>
    public CreateRoleCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Description).MaximumLength(512);
        RuleFor(c => c.Permissions).NotNull();

        // A role may only grant permissions some loaded module actually declared - otherwise a typo
        // creates a permission that no endpoint will ever check.
        RuleForEach(c => c.Permissions)
            .Must(BeADeclaredPermission)
            .WithMessage("'{PropertyValue}' is not a permission any loaded module declares.");
    }

    private static bool BeADeclaredPermission(string permission) =>
        PermissionConstants.All.Any(p => string.Equals(p.Value, permission, StringComparison.Ordinal));
}
