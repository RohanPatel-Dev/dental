using Dental.Framework.Core.Domain;

namespace Dental.Modules.Identity.Domain;

/// <summary>
/// One issued refresh token. Only a hash is stored, so a database leak does not hand out sessions.
/// </summary>
public sealed class RefreshToken : BaseEntity
{
    /// <summary>Owner of the token.</summary>
    public Guid UserId { get; set; }

    /// <summary>SHA-256 hash of the token value. The value itself is never persisted.</summary>
    public string TokenHash { get; set; } = default!;

    /// <summary>Identifier shared by every token in one rotation chain.</summary>
    public Guid SessionId { get; set; }

    /// <summary>When the token stops being accepted.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>When the token was used and rotated, if it has been.</summary>
    public DateTimeOffset? ConsumedAt { get; set; }

    /// <summary>When the token was revoked, if it has been.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Application the token was issued to, from the <c>X-App</c> header.</summary>
    public string App { get; set; } = default!;

    /// <summary>True when the token may still be exchanged.</summary>
    /// <param name="now">Current time, supplied by the caller's <c>TimeProvider</c>.</param>
    /// <returns><see langword="true"/> when the token is live.</returns>
    public bool IsUsable(DateTimeOffset now) =>
        ConsumedAt is null && RevokedAt is null && ExpiresAt > now;
}
