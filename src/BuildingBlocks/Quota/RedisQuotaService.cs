using System.Globalization;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Dental.Framework.Quota;

/// <summary>
/// Quota service backed by Redis counters, which is what makes the check-and-record atomic across
/// replicas.
/// </summary>
/// <param name="connectionMultiplexer">Shared Redis connection.</param>
/// <param name="limits">Resolves per-tenant limits.</param>
/// <param name="gauges">Gauge providers, one per gauge resource.</param>
/// <param name="options">Quota configuration.</param>
/// <param name="timeProvider">Clock. Never <c>DateTimeOffset.UtcNow</c>, so windows are testable.</param>
public sealed class RedisQuotaService(
    IConnectionMultiplexer connectionMultiplexer,
    IQuotaLimitProvider limits,
    IEnumerable<IQuotaGaugeProvider> gauges,
    IOptions<QuotaOptions> options,
    TimeProvider timeProvider) : IQuotaService
{
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

        IDatabase database = connectionMultiplexer.GetDatabase();
        (string key, DateTimeOffset windowEnd) = BuildWindowKey(tenantId, resource);

        long used = await database.StringIncrementAsync(key, units).ConfigureAwait(false);

        if (used == units)
        {
            await database.KeyExpireAsync(key, windowEnd.UtcDateTime).ConfigureAwait(false);
        }

        if (limit > 0 && used > limit)
        {
            // Roll the failed charge back so a refused request does not eat the tenant's budget.
            await database.StringDecrementAsync(key, units).ConfigureAwait(false);
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

        IDatabase database = connectionMultiplexer.GetDatabase();
        (string key, DateTimeOffset windowEnd) = BuildWindowKey(tenantId, resource);

        RedisValue value = await database.StringGetAsync(key).ConfigureAwait(false);
        long used = value.HasValue && value.TryParse(out long parsed) ? parsed : 0;

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
