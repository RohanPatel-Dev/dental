using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace Dental.Framework.Quota;

/// <summary>
/// Single process quota service for development and tests. Counters live in memory, so replicas do
/// not share them - never use this in a multi-replica deployment.
/// </summary>
/// <param name="limits">Resolves per-tenant limits.</param>
/// <param name="gauges">Gauge providers.</param>
/// <param name="options">Quota configuration.</param>
/// <param name="timeProvider">Clock.</param>
public sealed class InMemoryQuotaService(
    IQuotaLimitProvider limits,
    IEnumerable<IQuotaGaugeProvider> gauges,
    IOptions<QuotaOptions> options,
    TimeProvider timeProvider) : IQuotaService
{
    private readonly ConcurrentDictionary<string, long> _counters = new(StringComparer.Ordinal);
    private readonly QuotaOptions _options = options.Value;
    private readonly IReadOnlyList<IQuotaGaugeProvider> _gauges = [.. gauges];

    /// <inheritdoc />
    public async Task<QuotaUsage> CheckAndRecordAsync(
        string tenantId,
        QuotaResource resource,
        long units = 1,
        CancellationToken cancellationToken = default)
    {
        long limit = await ResolveLimitAsync(tenantId, resource, cancellationToken).ConfigureAwait(false);

        if (TryFindGauge(resource) is { } gauge)
        {
            long current = await gauge.GetCurrentAsync(tenantId, cancellationToken).ConfigureAwait(false);
            return new QuotaUsage(resource, current, limit, null);
        }

        (string key, DateTimeOffset windowEnd) = BuildWindowKey(tenantId, resource);

        long used = _counters.AddOrUpdate(key, units, (_, existing) => existing + units);

        if (limit > 0 && used > limit)
        {
            _counters.AddOrUpdate(key, 0, (_, existing) => existing - units);
            return new QuotaUsage(resource, limit, limit, windowEnd);
        }

        return new QuotaUsage(resource, used, limit, windowEnd);
    }

    /// <inheritdoc />
    public async Task<QuotaUsage> GetUsageAsync(
        string tenantId,
        QuotaResource resource,
        CancellationToken cancellationToken = default)
    {
        long limit = await ResolveLimitAsync(tenantId, resource, cancellationToken).ConfigureAwait(false);

        if (TryFindGauge(resource) is { } gauge)
        {
            long current = await gauge.GetCurrentAsync(tenantId, cancellationToken).ConfigureAwait(false);
            return new QuotaUsage(resource, current, limit, null);
        }

        (string key, DateTimeOffset windowEnd) = BuildWindowKey(tenantId, resource);
        _counters.TryGetValue(key, out long used);

        return new QuotaUsage(resource, used, limit, windowEnd);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<QuotaUsage>> GetAllUsageAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        List<QuotaUsage> usages = [];

        foreach (QuotaResource resource in Enum.GetValues<QuotaResource>())
        {
            usages.Add(await GetUsageAsync(tenantId, resource, cancellationToken).ConfigureAwait(false));
        }

        return usages;
    }

    private IQuotaGaugeProvider? TryFindGauge(QuotaResource resource) =>
        _gauges.FirstOrDefault(g => g.Resource == resource);

    private async Task<long> ResolveLimitAsync(
        string tenantId,
        QuotaResource resource,
        CancellationToken cancellationToken)
    {
        long? overridden = await limits.GetLimitAsync(tenantId, resource, cancellationToken)
            .ConfigureAwait(false);

        if (overridden is { } value)
        {
            return value;
        }

        return _options.DefaultLimits.TryGetValue(resource.ToString(), out long configured)
            ? configured
            : 0;
    }

    private (string Key, DateTimeOffset WindowEnd) BuildWindowKey(string tenantId, QuotaResource resource)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        long windowIndex = now.ToUnixTimeSeconds() / _options.WindowSeconds;
        DateTimeOffset windowEnd =
            DateTimeOffset.FromUnixTimeSeconds((windowIndex + 1) * _options.WindowSeconds);

        string key = string.Create(
            CultureInfo.InvariantCulture,
            $"quota:{tenantId}:{resource}:{windowIndex}");

        return (key, windowEnd);
    }
}
