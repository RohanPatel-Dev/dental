using System.Collections.Concurrent;

namespace Dental.Framework.Quota;

/// <summary>
/// Process-wide counter storage for <see cref="InMemoryQuotaService"/>.
/// </summary>
/// <remarks>
/// Separated from the service so the service itself can be SCOPED: it consumes scoped limit and
/// gauge providers, which a singleton cannot. The counters still have to outlive a request, so they
/// live here in a singleton instead.
/// Counters are per process, so this is for development and tests only - replicas do not share them.
/// </remarks>
public sealed class InMemoryQuotaCounterStore
{
    private readonly ConcurrentDictionary<string, long> _counters = new(StringComparer.Ordinal);

    /// <summary>Adds to a counter and returns the new value.</summary>
    /// <param name="key">Window scoped counter key.</param>
    /// <param name="units">Units to add. May be negative, to roll a refused charge back.</param>
    /// <returns>The value after the change.</returns>
    public long Add(string key, long units) =>
        _counters.AddOrUpdate(key, units, (_, existing) => existing + units);

    /// <summary>Reads a counter without changing it.</summary>
    /// <param name="key">Window scoped counter key.</param>
    /// <returns>The current value, or zero.</returns>
    public long Read(string key)
    {
        _counters.TryGetValue(key, out long value);
        return value;
    }
}
