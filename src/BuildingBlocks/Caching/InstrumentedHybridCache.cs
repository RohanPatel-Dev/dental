using System.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Framework.Caching;

/// <summary>
/// Decorates <see cref="HybridCache"/> with an activity per operation and a factory-invocation
/// counter, so a runaway cache miss rate is visible in traces instead of only in latency.
/// </summary>
/// <param name="inner">The real cache implementation.</param>
public sealed class InstrumentedHybridCache(HybridCache inner) : HybridCache
{
    /// <inheritdoc />
    public override ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);

        using Activity? activity = CacheTelemetry.ActivitySource.StartActivity("cache.get_or_create");
        activity?.SetTag("cache.key", key);
        CacheTelemetry.Operations.Add(1, new KeyValuePair<string, object?>("operation", "get_or_create"));

        return inner.GetOrCreateAsync(
            key,
            state,
            (innerState, ct) =>
            {
                CacheTelemetry.Misses.Add(1);
                Activity.Current?.SetTag("cache.hit", false);
                return factory(innerState, ct);
            },
            options,
            tags,
            cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        CacheTelemetry.Operations.Add(1, new KeyValuePair<string, object?>("operation", "set"));
        return inner.SetAsync(key, value, options, tags, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        CacheTelemetry.Operations.Add(1, new KeyValuePair<string, object?>("operation", "remove"));
        return inner.RemoveAsync(key, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask RemoveAsync(
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        CacheTelemetry.Operations.Add(1, new KeyValuePair<string, object?>("operation", "remove_many"));
        return inner.RemoveAsync(keys, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        CacheTelemetry.Operations.Add(1, new KeyValuePair<string, object?>("operation", "remove_by_tag"));
        return inner.RemoveByTagAsync(tag, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask RemoveByTagAsync(
        IEnumerable<string> tags,
        CancellationToken cancellationToken = default)
    {
        CacheTelemetry.Operations.Add(1, new KeyValuePair<string, object?>("operation", "remove_by_tags"));
        return inner.RemoveByTagAsync(tags, cancellationToken);
    }
}
