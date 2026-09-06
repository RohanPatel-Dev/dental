using Dental.Modules.Identity.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Identity.Contracts.v1.Users.SetUserStatus;

/// <summary>Activates or deactivates a user.</summary>
/// <param name="UserId">User identifier.</param>
/// <param name="IsActive">Target state.</param>
public sealed record SetUserStatusCommand(Guid UserId, bool IsActive) : ICommand<UserDto>;
