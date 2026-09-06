using System.ComponentModel.DataAnnotations;

namespace Dental.Framework.Caching;

/// <summary>Caching configuration, bound from the <c>CachingOptions</c> section.</summary>
public sealed class CachingOptions
{
    /// <summary>
    /// Redis (or Valkey) connection string. When empty the cache is L1 only, which is correct for a
    /// single node but leaves peers stale after an invalidation.
    /// </summary>
    public string? Redis { get; set; }

    /// <summary>Total lifetime of an entry across L1 and L2.</summary>
    [Range(1, 86400)]
    public int DefaultExpirationSeconds { get; set; } = 3600;

    /// <summary>
    /// Lifetime of the L1 copy. There is no L1 backplane: tag invalidation does not evict L1 on peer
    /// nodes, so cross-node staleness is bounded only by this value. Keep it short for hot mutable data.
    /// </summary>
    [Range(1, 3600)]
    public int LocalExpirationSeconds { get; set; } = 120;

    /// <summary>Largest payload that will be cached, in bytes.</summary>
    [Range(1024, 10 * 1024 * 1024)]
    public int MaximumPayloadBytes { get; set; } = 1024 * 1024;

    /// <summary>Longest key that will be cached, in characters.</summary>
    [Range(64, 4096)]
    public int MaximumKeyLength { get; set; } = 1024;

    /// <summary>Prefix applied to every Redis key so several apps can share one server.</summary>
    public string InstanceName { get; set; } = "dental:";
}
