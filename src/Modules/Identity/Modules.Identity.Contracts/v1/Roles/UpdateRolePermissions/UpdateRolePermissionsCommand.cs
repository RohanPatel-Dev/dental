using Dental.Modules.Identity.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Identity.Contracts.v1.Roles.UpdateRolePermissions;

/// <summary>Replaces the permission set of a role.</summary>
/// <param name="RoleId">Role identifier.</param>
/// <param name="Permissions">The complete set of permissions the role should grant.</param>
public sealed record UpdateRolePermissionsCommand(Guid RoleId, IReadOnlyList<string> Permissions)
    : ICommand<RoleDto>;
