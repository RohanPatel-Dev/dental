using Dental.Framework.Quota;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Dental.Framework.Tests.Quota;

/// <summary>
/// Metering rules. The two that matter operationally: a refused charge must not consume the
/// tenant's budget, and counters must be scoped to both the tenant and the window.
/// </summary>
public sealed class InMemoryQuotaServiceTests
{
    private const string Tenant = "acme-dental";

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 9, 7, 9, 0, 0, TimeSpan.Zero));
    private readonly InMemoryQuotaCounterStore _counters = new();

    #region Happy Path

    [Fact]
    public async Task CheckAndRecordAsync_Should_AccumulateAcrossCalls()
    {
        InMemoryQuotaService service = Service(limit: 10);

        await service.CheckAndRecordAsync(Tenant, QuotaResource.ApiCalls, 3, TestContext.Current.CancellationToken);
        QuotaUsage usage = await service.CheckAndRecordAsync(
            Tenant, QuotaResource.ApiCalls, 4, TestContext.Current.CancellationToken);

        usage.Used.ShouldBe(7);
        usage.Limit.ShouldBe(10);
        usage.Remaining.ShouldBe(3);
        usage.IsExceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task GetUsageAsync_Should_ReportWithoutConsuming()
    {
        InMemoryQuotaService service = Service(limit: 10);
        await service.CheckAndRecordAsync(Tenant, QuotaResource.ApiCalls, 2, TestContext.Current.CancellationToken);

        await service.GetUsageAsync(Tenant, QuotaResource.ApiCalls, TestContext.Current.CancellationToken);
        QuotaUsage usage = await service.GetUsageAsync(
            Tenant, QuotaResource.ApiCalls, TestContext.Current.CancellationToken);

        usage.Used.ShouldBe(2);
    }

    [Fact]
    public async Task CheckAndRecordAsync_Should_ReadAGauge_When_AProviderCoversTheResource()
    {
        // Gauges report a level (patients on file), so they are never incremented by a request.
        IQuotaGaugeProvider gauge = Substitute.For<IQuotaGaugeProvider>();
        gauge.Resource.Returns(QuotaResource.Patients);
        gauge.GetCurrentAsync(Tenant, Arg.Any<CancellationToken>()).Returns(42L);

        InMemoryQuotaService service = Service(limit: 100, gauges: [gauge]);

        QuotaUsage usage = await service.CheckAndRecordAsync(
            Tenant, QuotaResource.Patients, 1, TestContext.Current.CancellationToken);

        usage.Used.ShouldBe(42);
        usage.WindowEndsAt.ShouldBeNull();
        _counters.Read($"quota:{Tenant}:{QuotaResource.Patients}:0").ShouldBe(0);
    }

    [Fact]
    public async Task GetAllUsageAsync_Should_CoverEveryResource()
    {
        IReadOnlyCollection<QuotaUsage> usages =
            await Service(limit: 10).GetAllUsageAsync(Tenant, TestContext.Current.CancellationToken);

        usages.Select(u => u.Resource).ShouldBe(Enum.GetValues<QuotaResource>(), ignoreOrder: true);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CheckAndRecordAsync_Should_RollTheChargeBack_When_ItIsRefused()
    {
        // A refused request must not eat budget, or a client retrying against a full quota would
        // push the counter further and further past the limit.
        InMemoryQuotaService service = Service(limit: 5);
        await service.CheckAndRecordAsync(Tenant, QuotaResource.ApiCalls, 4, TestContext.Current.CancellationToken);

        QuotaUsage refused = await service.CheckAndRecordAsync(
            Tenant, QuotaResource.ApiCalls, 5, TestContext.Current.CancellationToken);

        refused.IsExceeded.ShouldBeTrue();
        refused.Used.ShouldBe(5);

        QuotaUsage actual = await service.GetUsageAsync(
            Tenant, QuotaResource.ApiCalls, TestContext.Current.CancellationToken);
        actual.Used.ShouldBe(4);

        // And the units that do still fit are accepted afterwards.
        QuotaUsage accepted = await service.CheckAndRecordAsync(
            Tenant, QuotaResource.ApiCalls, 1, TestContext.Current.CancellationToken);
        accepted.IsExceeded.ShouldBeTrue();
        accepted.Used.ShouldBe(5);
    }

    [Fact]
    public async Task CheckAndRecordAsync_Should_KeepTenantsApart()
    {
        InMemoryQuotaService service = Service(limit: 10);

        await service.CheckAndRecordAsync(Tenant, QuotaResource.ApiCalls, 6, TestContext.Current.CancellationToken);
        QuotaUsage other = await service.CheckAndRecordAsync(
            "other-practice", QuotaResource.ApiCalls, 1, TestContext.Current.CancellationToken);

        other.Used.ShouldBe(1);
    }

    [Fact]
    public async Task CheckAndRecordAsync_Should_StartAFreshCounter_InTheNextWindow()
    {
        InMemoryQuotaService service = Service(limit: 10, windowSeconds: 3600);
        await service.CheckAndRecordAsync(Tenant, QuotaResource.ApiCalls, 9, TestContext.Current.CancellationToken);

        _clock.Advance(TimeSpan.FromHours(1));

        QuotaUsage usage = await service.CheckAndRecordAsync(
            Tenant, QuotaResource.ApiCalls, 1, TestContext.Current.CancellationToken);

        usage.Used.ShouldBe(1);
        usage.WindowEndsAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task CheckAndRecordAsync_Should_NeverRefuse_When_TheLimitIsZero()
    {
        // Zero means unlimited, not "nothing allowed" - the difference between a working system and
        // a tenant that can do nothing at all.
        InMemoryQuotaService service = Service(limit: 0);

        QuotaUsage usage = await service.CheckAndRecordAsync(
            Tenant, QuotaResource.ApiCalls, 1_000_000, TestContext.Current.CancellationToken);

        usage.IsExceeded.ShouldBeFalse();
        usage.Remaining.ShouldBe(long.MaxValue);
    }

    [Fact]
    public async Task CheckAndRecordAsync_Should_PreferThePlanLimit_OverTheConfiguredDefault()
    {
        IQuotaLimitProvider limits = Substitute.For<IQuotaLimitProvider>();
        limits.GetLimitAsync(Tenant, QuotaResource.ApiCalls, Arg.Any<CancellationToken>())
            .Returns((long?)3);

        InMemoryQuotaService service = Service(limit: 1000, limits: limits);

        QuotaUsage usage = await service.CheckAndRecordAsync(
            Tenant, QuotaResource.ApiCalls, 3, TestContext.Current.CancellationToken);

        usage.Limit.ShouldBe(3);
        usage.IsExceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckAndRecordAsync_Should_ReportUnlimited_When_NothingConfiguresTheResource()
    {
        InMemoryQuotaService service = Service(limits: new NoQuotaLimitProvider(), defaults: EmptyDefaults);

        QuotaUsage usage = await service.CheckAndRecordAsync(
            Tenant, QuotaResource.Notifications, 1, TestContext.Current.CancellationToken);

        usage.Limit.ShouldBe(0);
        usage.IsExceeded.ShouldBeFalse();
    }

    #endregion

    private static readonly IReadOnlyDictionary<string, long> EmptyDefaults =
        new Dictionary<string, long>(StringComparer.Ordinal);

    private InMemoryQuotaService Service(
        long limit = 100,
        int windowSeconds = 86400,
        IQuotaLimitProvider? limits = null,
        IEnumerable<IQuotaGaugeProvider>? gauges = null,
        IReadOnlyDictionary<string, long>? defaults = null)
    {
        QuotaOptions options = new() { Enabled = true, WindowSeconds = windowSeconds };
        options.DefaultLimits.Clear();

        foreach (KeyValuePair<string, long> pair in defaults
            ?? Enum.GetValues<QuotaResource>().ToDictionary(r => r.ToString(), _ => limit))
        {
            options.DefaultLimits[pair.Key] = pair.Value;
        }

        return new InMemoryQuotaService(
            _counters,
            limits ?? new NoQuotaLimitProvider(),
            gauges ?? [],
            Options.Create(options),
            _clock);
    }
}
