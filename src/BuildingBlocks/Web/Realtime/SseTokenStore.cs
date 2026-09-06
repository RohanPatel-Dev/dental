using System.Collections.Concurrent;

namespace Dental.Framework.Web.Realtime;

/// <summary>
/// Single-use, short lived tokens that let an <c>EventSource</c> authenticate.
/// </summary>
/// <remarks>
/// The browser's <c>EventSource</c> cannot send an <c>Authorization</c> header, so the SPA POSTs to
/// the authorized token endpoint, receives a GUID valid for 30 seconds, and passes it on the
/// anonymous stream URL. Consuming a token removes it.
/// </remarks>
/// <param name="timeProvider">Clock.</param>
public sealed class SseTokenStore(TimeProvider timeProvider)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<Guid, Entry> _tokens = new();

    /// <summary>Issues a token for a user.</summary>
    /// <param name="userId">The authenticated user.</param>
    /// <returns>The single-use token.</returns>
    public Guid Issue(Guid userId)
    {
        Guid token = Guid.CreateVersion7();
        _tokens[token] = new Entry(userId, timeProvider.GetUtcNow().Add(Lifetime));
        Sweep();
        return token;
    }

    /// <summary>Consumes a token, returning the user it was issued to.</summary>
    /// <param name="token">The token from the query string.</param>
    /// <param name="userId">The user, when the token was valid.</param>
    /// <returns><see langword="true"/> when the token was valid and unused.</returns>
    public bool TryConsume(Guid token, out Guid userId)
    {
        userId = Guid.Empty;

        if (!_tokens.TryRemove(token, out Entry entry))
        {
            return false;
        }

        if (entry.ExpiresAt < timeProvider.GetUtcNow())
        {
            return false;
        }

        userId = entry.UserId;
        return true;
    }

    private void Sweep()
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        foreach (KeyValuePair<Guid, Entry> pair in _tokens)
        {
            if (pair.Value.ExpiresAt < now)
            {
                _tokens.TryRemove(pair.Key, out _);
            }
        }
    }

    private readonly record struct Entry(Guid UserId, DateTimeOffset ExpiresAt);
}
