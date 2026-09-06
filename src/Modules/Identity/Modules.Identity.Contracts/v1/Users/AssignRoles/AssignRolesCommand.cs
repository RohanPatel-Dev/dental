using Dental.Modules.Identity.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Identity.Contracts.v1.Users.AssignRoles;

/// <summary>Replaces a user's role set.</summary>
/// <param name="UserId">User identifier.</param>
/// <param name="Roles">The complete set of roles the user should hold.</param>
public sealed record AssignRolesCommand(Guid UserId, IReadOnlyList<string> Roles) : ICommand<UserDto>;
