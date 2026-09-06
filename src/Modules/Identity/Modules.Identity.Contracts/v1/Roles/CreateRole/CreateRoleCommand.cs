using Dental.Modules.Identity.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Identity.Contracts.v1.Roles.CreateRole;

/// <summary>Creates a role in the current tenant.</summary>
/// <param name="Name">Role name.</param>
/// <param name="Description">What the role is for.</param>
/// <param name="Permissions">Permission values granted.</param>
public sealed record CreateRoleCommand(
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions) : ICommand<RoleDto>;
