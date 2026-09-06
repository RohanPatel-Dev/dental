using Dental.Framework.Core.Contracts;
using Dental.Framework.Core.Exceptions;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.Services;
using Dental.Modules.Identity.Contracts.v1.Account.GetCurrentUser;
using Mediator;

namespace Dental.Modules.Identity.Features.v1.Account.GetCurrentUser;

/// <summary>Reads the signed-in user's profile and effective permissions.</summary>
/// <param name="currentUser">The caller.</param>
/// <param name="userService">User and permission lookups.</param>
public sealed class GetCurrentUserQueryHandler(ICurrentUser currentUser, IUserService userService)
    : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    /// <inheritdoc />
    public async ValueTask<CurrentUserDto> Handle(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            throw new UnauthorizedException("The request is not authenticated.");
        }

        UserDto user = await userService.GetAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("User", userId);

        IReadOnlyList<string> permissions = await userService
            .GetPermissionsAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return new CurrentUserDto(user, permissions, currentUser.TenantId ?? string.Empty);
    }
}
