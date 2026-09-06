namespace Dental.Framework.Quota;

/// <summary>Current consumption of one resource for one tenant.</summary>
/// <param name="Resource">The metered resource.</param>
/// <param name="Used">Units consumed.</param>
/// <param name="Limit">Units allowed. Zero or less means unlimited.</param>
/// <param name="WindowEndsAt">When a counter window resets. Null for gauges.</param>
public sealed record QuotaUsage(
    QuotaResource Resource,
    long Used,
    long Limit,
    DateTimeOffset? WindowEndsAt)
{
    /// <summary>True when consumption has reached the limit.</summary>
    public bool IsExceeded => Limit > 0 && Used >= Limit;

    /// <summary>Units still available, or <see cref="long.MaxValue"/> when unlimited.</summary>
    public long Remaining => Limit <= 0 ? long.MaxValue : Math.Max(0, Limit - Used);
}
