using Dental.Framework.Core.Exceptions;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Tokens.IssueToken;
using Dental.Modules.Identity.Data;
using Dental.Modules.Identity.Domain;
using Dental.Modules.Identity.Services;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using DomainRefreshToken = Dental.Modules.Identity.Domain.RefreshToken;

namespace Dental.Modules.Identity.Features.v1.Tokens.IssueToken;

/// <summary>Exchanges credentials for a token pair.</summary>
/// <param name="userManager">ASP.NET Identity user manager.</param>
/// <param name="context">The identity context.</param>
/// <param name="tokenService">Mints tokens.</param>
/// <param name="userService">Reads role names.</param>
/// <param name="tenantContextAccessor">Supplies the resolved tenant.</param>
/// <param name="timeProvider">Clock.</param>
/// <param name="logger">Logger.</param>
public sealed class IssueTokenCommandHandler(
    UserManager<DentalUser> userManager,
    IdentityModuleDbContext context,
    TokenService tokenService,
    UserService userService,
    IMultiTenantContextAccessor tenantContextAccessor,
    TimeProvider timeProvider,
    ILogger<IssueTokenCommandHandler> logger) : ICommandHandler<IssueTokenCommand, TokenDto>
{
    /// <inheritdoc />
    public async ValueTask<TokenDto> Handle(IssueTokenCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string? tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new UnauthorizedException("No tenant was supplied with the request.");
        }

        DentalUser? user = await userManager.FindByEmailAsync(command.Email).ConfigureAwait(false);

        // One failure message for every rejection path: distinguishing "no such user" from "wrong
        // password" turns the sign-in form into an account enumeration oracle.
        if (user is null
            || !user.IsActive
            || !await userManager.CheckPasswordAsync(user, command.Password).ConfigureAwait(false))
        {
            logger.LogWarning(
                "Rejected sign-in for {Email} in tenant {TenantId}.",
                command.Email,
                tenantId);

            throw new UnauthorizedException("The email address or password is incorrect.");
        }

        Guid sessionId = Guid.CreateVersion7();
        IReadOnlyList<string> roles = await userService.GetRoleNamesAsync(user.Id, cancellationToken)
            .ConfigureAwait(false);

        (string accessToken, DateTimeOffset expiresAt) =
            tokenService.CreateAccessToken(user, roles, sessionId, command.App);

        (string refreshToken, DomainRefreshToken record) =
            tokenService.CreateRefreshToken(user, sessionId, command.App);

        context.RefreshTokens.Add(record);
        user.LastLoginAt = timeProvider.GetUtcNow();

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Issued a token for user {UserId} in tenant {TenantId} for app {App}.",
            user.Id,
            tenantId,
            command.App);

        return new TokenDto(accessToken, refreshToken, expiresAt, record.ExpiresAt);
    }
}
