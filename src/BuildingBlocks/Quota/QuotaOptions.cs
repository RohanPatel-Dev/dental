using System.ComponentModel.DataAnnotations;

namespace Dental.Framework.Quota;

/// <summary>Quota configuration, bound from the <c>QuotaOptions</c> section.</summary>
public sealed class QuotaOptions
{
    /// <summary>Whether quota enforcement is active. Read eagerly at registration.</summary>
    public bool Enabled { get; set; }

    /// <summary>Length of a counter window in seconds.</summary>
    [Range(60, 2678400)]
    public int WindowSeconds { get; set; } = 86400;

    /// <summary>Limits applied when a tenant's plan does not override them.</summary>
    public IDictionary<string, long> DefaultLimits { get; } = new Dictionary<string, long>(StringComparer.Ordinal)
    {
        [nameof(QuotaResource.ApiCalls)] = 250_000,
        [nameof(QuotaResource.StorageBytes)] = 50L * 1024 * 1024 * 1024,
        [nameof(QuotaResource.Users)] = 100,
        [nameof(QuotaResource.Patients)] = 25_000,
        [nameof(QuotaResource.Notifications)] = 20_000,
    };
}
