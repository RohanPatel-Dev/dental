using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Framework.Caching;

/// <summary>Ergonomic wrappers over <see cref="HybridCache"/>.</summary>
public static class HybridCacheExtensions
{
    /// <summary>
    /// Fetches an entry, producing it with <paramref name="factory"/> on a miss. Concurrent callers
    /// for the same key share one factory invocation.
    /// </summary>
    /// <typeparam name="T">Cached value type.</typeparam>
    /// <param name="cache">The cache.</param>
    /// <param name="key">Key from <c>CacheKeys</c> - never an inline string.</param>
    /// <param name="factory">Produces the value on a miss.</param>
    /// <param name="tags">Tags used for bulk invalidation.</param>
    /// <param name="expiration">Total lifetime override.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached or freshly produced value.</returns>
    public static ValueTask<T> GetOrCreateAsync<T>(
        this HybridCache cache,
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        IEnumerable<string>? tags = null,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cache);

        HybridCacheEntryOptions? options = expiration is null
            ? null
            : new HybridCacheEntryOptions { Expiration = expiration };

        return cache.GetOrCreateAsync(key, factory, options, tags, cancellationToken);
    }
}
