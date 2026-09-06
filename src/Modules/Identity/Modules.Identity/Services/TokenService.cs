using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dental.Framework.Shared.Identity;
using Dental.Modules.Identity.Domain;
using Dental.Framework.Web.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Dental.Modules.Identity.Services;

/// <summary>Mints access tokens and the opaque refresh tokens that rotate them.</summary>
/// <param name="jwtOptions">JWT configuration.</param>
/// <param name="timeProvider">Clock.</param>
public sealed class TokenService(IOptions<JwtOptions> jwtOptions, TimeProvider timeProvider)
{
    private readonly JwtOptions _options = jwtOptions.Value;

    /// <summary>Builds a signed access token.</summary>
    /// <param name="user">The signed-in user.</param>
    /// <param name="roles">Role names to embed.</param>
    /// <param name="sessionId">Refresh token chain identifier.</param>
    /// <param name="app">Requesting application.</param>
    /// <returns>The token and its expiry.</returns>
    /// <remarks>
    /// Only ROLE names go into the token, never the expanded permission set. Permissions change
    /// often and a token cannot be un-issued; the SPA fetches them separately from
    /// <c>/account/me</c> instead.
    /// </remarks>
    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(
        DentalUser user,
        IReadOnlyCollection<string> roles,
        Guid sessionId,
        string app)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);

        DateTimeOffset expiresAt = timeProvider.GetUtcNow()
            .AddMinutes(_options.AccessTokenExpirationMinutes);

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(DentalClaims.FullName, user.FullName),
            new(DentalClaims.Tenant, user.TenantId),
            new(DentalClaims.App, app),
            new(DentalClaims.SessionId, sessionId.ToString()),
            .. roles.Select(role => new Claim(ClaimTypes.Role, role)),
        ];

        SigningCredentials credentials = new(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: timeProvider.GetUtcNow().UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <summary>Creates a refresh token and the row that records its hash.</summary>
    /// <param name="user">The signed-in user.</param>
    /// <param name="sessionId">Chain identifier, preserved across rotations.</param>
    /// <param name="app">Requesting application.</param>
    /// <returns>The clear text token to return, and the row to persist.</returns>
    public (string Token, RefreshToken Record) CreateRefreshToken(
        DentalUser user,
        Guid sessionId,
        string app)
    {
        ArgumentNullException.ThrowIfNull(user);

        string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        RefreshToken record = new()
        {
            UserId = user.Id,
            TokenHash = Hash(token),
            SessionId = sessionId,
            ExpiresAt = timeProvider.GetUtcNow().AddDays(_options.RefreshTokenExpirationDays),
            App = app,
            TenantId = user.TenantId,
        };

        return (token, record);
    }

    /// <summary>Hashes a refresh token for lookup and comparison.</summary>
    /// <param name="token">Clear text token.</param>
    /// <returns>The lower case hex SHA-256 hash.</returns>
    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
