using Dental.Framework.Core.Exceptions;
using Dental.Framework.Shared.Caching;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Users.AssignRoles;
using Dental.Modules.Identity.Domain;
using Dental.Modules.Identity.Services;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Modules.Identity.Features.v1.Users.AssignRoles;

/// <summary>Replaces a user's role set.</summary>
/// <param name="userManager">ASP.NET Identity user manager.</param>
/// <param name="cache">Shared cache, invalidated after the write.</param>
public sealed class AssignRolesCommandHandler(UserManager<DentalUser> userManager, HybridCache cache)
    : ICommandHandler<AssignRolesCommand, UserDto>
{
    /// <inheritdoc />
    public async ValueTask<UserDto> Handle(AssignRolesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        DentalUser user = await userManager.FindByIdAsync(command.UserId.ToString()).ConfigureAwait(false)
            ?? throw NotFoundException.For("User", command.UserId);

        IList<string> current = await userManager.GetRolesAsync(user).ConfigureAwait(false);

        string[] toRemove = [.. current.Except(command.Roles, StringComparer.Ordinal)];
        string[] toAdd = [.. command.Roles.Except(current, StringComparer.Ordinal)];

        if (toRemove.Length > 0)
        {
            Check(await userManager.RemoveFromRolesAsync(user, toRemove).ConfigureAwait(false));
        }

        if (toAdd.Length > 0)
        {
            Check(await userManager.AddToRolesAsync(user, toAdd).ConfigureAwait(false));
        }

        // The user's cached permission set is derived from their roles, so it is now stale.
        await cache.RemoveAsync(CacheKeys.IdentityKeys.UserPermissions(user.Id), cancellationToken)
            .ConfigureAwait(false);

        return UserService.Map(user, command.Roles);
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new CustomException(
                "The roles could not be updated.",
                [.. result.Errors.Select(e => e.Description)],
                System.Net.HttpStatusCode.BadRequest);
        }
    }
}
