using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Dental.Framework.Caching;

/// <summary>Activity source and meter used by <see cref="InstrumentedHybridCache"/>.</summary>
public static class CacheTelemetry
{
    /// <summary>Name of the activity source and meter.</summary>
    public const string Name = "Dental.Caching";

    /// <summary>Activity source for cache spans.</summary>
    public static readonly ActivitySource ActivitySource = new(Name);

    /// <summary>Meter for cache counters.</summary>
    public static readonly Meter Meter = new(Name);

    /// <summary>Counts cache operations, tagged by operation name.</summary>
    public static readonly Counter<long> Operations =
        Meter.CreateCounter<long>("dental.cache.operations", "count", "Cache operations by kind.");

    /// <summary>Counts factory invocations, i.e. entries that had to be produced.</summary>
    public static readonly Counter<long> Misses =
        Meter.CreateCounter<long>("dental.cache.misses", "count", "Entries produced by the factory.");
}
