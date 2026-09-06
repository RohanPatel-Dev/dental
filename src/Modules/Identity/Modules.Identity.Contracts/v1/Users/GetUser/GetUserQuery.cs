using Dental.Modules.Identity.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Identity.Contracts.v1.Users.GetUser;

/// <summary>Reads one user in the current tenant.</summary>
/// <param name="UserId">User identifier.</param>
public sealed record GetUserQuery(Guid UserId) : IQuery<UserDto>;
