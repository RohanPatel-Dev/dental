using Dental.Framework.Core.Contracts;
using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Framework.Shared.Caching;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.Events;
using Dental.Modules.Identity.Contracts.v1.Users.SetUserStatus;
using Dental.Modules.Identity.Data;
using Dental.Modules.Identity.Domain;
using Dental.Modules.Identity.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Modules.Identity.Features.v1.Users.SetUserStatus;

/// <summary>Activates or deactivates a user and revokes their live sessions.</summary>
/// <param name="context">The identity context.</param>
/// <param name="userService">Reads role names.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="currentUser">The caller, so they cannot lock themselves out.</param>
/// <param name="timeProvider">Clock.</param>
/// <param name="cache">Shared cache, invalidated after the write.</param>
public sealed class SetUserStatusCommandHandler(
    IdentityModuleDbContext context,
    UserService userService,
    IOutboxStore<IdentityModuleDbContext> outbox,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    HybridCache cache) : ICommandHandler<SetUserStatusCommand, UserDto>
{
    /// <inheritdoc />
    public async ValueTask<UserDto> Handle(
        SetUserStatusCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!command.IsActive && currentUser.UserId == command.UserId)
        {
            throw new ForbiddenException("You cannot deactivate your own account.");
        }

        DentalUser user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("User", command.UserId);

        user.IsActive = command.IsActive;

        if (!command.IsActive)
        {
            // Deactivation has to end live sessions too: the access token stays valid until it
            // expires, but without a refresh token the session cannot outlive it.
            await context.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ExecuteUpdateAsync(
                    update => update.SetProperty(t => t.RevokedAt, timeProvider.GetUtcNow()),
                    cancellationToken)
                .ConfigureAwait(false);

            await outbox.AddAsync(
                    new UserDeactivatedIntegrationEvent(user.Id)
                    {
                        TenantId = user.TenantId,
                        Source = nameof(Identity),
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(CacheKeys.Tags.Identity, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<string> roles = await userService.GetRoleNamesAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        return UserService.Map(user, roles);
    }
}
