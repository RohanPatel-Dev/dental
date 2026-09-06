using Dental.Framework.Core.Exceptions;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.Services;
using Dental.Modules.Identity.Contracts.v1.Users.GetUser;
using Mediator;

namespace Dental.Modules.Identity.Features.v1.Users.GetUser;

/// <summary>Reads one user in the current tenant.</summary>
/// <param name="userService">User lookups.</param>
public sealed class GetUserQueryHandler(IUserService userService) : IQueryHandler<GetUserQuery, UserDto>
{
    /// <inheritdoc />
    public async ValueTask<UserDto> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // The tenant query filter is what makes this a 404 rather than a leak: another tenant's user
        // is simply not visible to this query.
        return await userService.GetAsync(query.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("User", query.UserId);
    }
}
