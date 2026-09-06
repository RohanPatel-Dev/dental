using Dental.Framework.Shared.Identity;
using Dental.Modules.Identity.Contracts.v1.Roles.UpdateRolePermissions;
using FluentValidation;

namespace Dental.Modules.Identity.Features.v1.Roles.UpdateRolePermissions;

/// <summary>Validates <see cref="UpdateRolePermissionsCommand"/>.</summary>
public sealed class UpdateRolePermissionsCommandValidator
    : AbstractValidator<UpdateRolePermissionsCommand>
{
    /// <summary>Builds the rules.</summary>
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(c => c.RoleId).NotEmpty();
        RuleFor(c => c.Permissions).NotNull();

        RuleForEach(c => c.Permissions)
            .Must(BeADeclaredPermission)
            .WithMessage("'{PropertyValue}' is not a permission any loaded module declares.");
    }

    private static bool BeADeclaredPermission(string permission) =>
        PermissionConstants.All.Any(p => string.Equals(p.Value, permission, StringComparison.Ordinal));
}
