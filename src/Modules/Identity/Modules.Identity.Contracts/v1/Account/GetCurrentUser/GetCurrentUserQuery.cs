using Dental.Modules.Identity.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Identity.Contracts.v1.Account.GetCurrentUser;

/// <summary>
/// Reads the signed-in user's profile and effective permissions.
/// </summary>
/// <remarks>
/// The JWT deliberately carries role names only. The permission set is fetched here so that a role
/// permission change takes effect on the next page load rather than at the next token refresh.
/// </remarks>
public sealed record GetCurrentUserQuery : IQuery<CurrentUserDto>;
