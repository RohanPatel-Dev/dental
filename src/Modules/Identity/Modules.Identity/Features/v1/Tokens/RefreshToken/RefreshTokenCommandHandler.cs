using Dental.Framework.Core.Exceptions;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Tokens.RefreshToken;
using Dental.Modules.Identity.Data;
using Dental.Modules.Identity.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DomainRefreshToken = Dental.Modules.Identity.Domain.RefreshToken;

namespace Dental.Modules.Identity.Features.v1.Tokens.RefreshToken;

/// <summary>Rotates a refresh token, issuing a fresh pair.</summary>
/// <param name="context">The identity context.</param>
/// <param name="tokenService">Mints tokens.</param>
/// <param name="userService">Reads role names.</param>
/// <param name="timeProvider">Clock.</param>
/// <param name="logger">Logger.</param>
public sealed class RefreshTokenCommandHandler(
    IdentityModuleDbContext context,
    TokenService tokenService,
    UserService userService,
    TimeProvider timeProvider,
    ILogger<RefreshTokenCommandHandler> logger) : ICommandHandler<RefreshTokenCommand, TokenDto>
{
    /// <inheritdoc />
    public async ValueTask<TokenDto> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string hash = TokenService.Hash(command.RefreshToken);
        DateTimeOffset now = timeProvider.GetUtcNow();

        DomainRefreshToken? record = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new UnauthorizedException("The refresh token is not valid.");
        }

        if (!record.IsUsable(now))
        {
            // A token that was already consumed being presented again means the value leaked, so the
            // whole rotation chain is burned rather than just this one token.
            await RevokeSessionAsync(record, now, cancellationToken).ConfigureAwait(false);

            logger.LogWarning(
                "Refresh token reuse detected for user {UserId}; session {SessionId} revoked.",
                record.UserId,
                record.SessionId);

            throw new UnauthorizedException("The refresh token is not valid.");
        }

        Domain.DentalUser user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == record.UserId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new UnauthorizedException("The refresh token is not valid.");

        if (!user.IsActive)
        {
            throw new UnauthorizedException("The account is not active.");
        }

        record.ConsumedAt = now;

        IReadOnlyList<string> roles = await userService.GetRoleNamesAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        (string accessToken, DateTimeOffset expiresAt) =
            tokenService.CreateAccessToken(user, roles, record.SessionId, record.App);

        (string refreshToken, DomainRefreshToken next) =
            tokenService.CreateRefreshToken(user, record.SessionId, record.App);

        context.RefreshTokens.Add(next);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TokenDto(accessToken, refreshToken, expiresAt, next.ExpiresAt);
    }

    private async Task RevokeSessionAsync(
        DomainRefreshToken record,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await context.RefreshTokens
            .Where(t => t.SessionId == record.SessionId && t.RevokedAt == null)
            .ExecuteUpdateAsync(
                update => update.SetProperty(t => t.RevokedAt, now),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
