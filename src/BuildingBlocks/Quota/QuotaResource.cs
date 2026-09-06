namespace Dental.Framework.Quota;

/// <summary>
/// A metered resource. Counters accumulate over a window and reset; gauges report a current level
/// supplied by a provider.
/// </summary>
public enum QuotaResource
{
    /// <summary>Counter: API requests served for the tenant in the current window.</summary>
    ApiCalls = 0,

    /// <summary>Gauge: bytes the tenant currently occupies in object storage.</summary>
    StorageBytes = 1,

    /// <summary>Gauge: active users in the tenant.</summary>
    Users = 2,

    /// <summary>Gauge: patient records in the tenant.</summary>
    Patients = 3,

    /// <summary>Counter: outbound messages sent in the current window.</summary>
    Notifications = 4,
}
