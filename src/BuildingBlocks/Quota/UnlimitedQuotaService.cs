namespace Dental.Framework.Quota;

/// <summary>
/// Quota service for hosts that do not enforce quotas: it records nothing and refuses nothing.
/// </summary>
/// <remarks>
/// Registered instead of a real implementation when <c>EnableQuotas</c> is off. Feature code still
/// depends on <see cref="IQuotaService"/> unconditionally, so the abstraction has to exist in every
/// host - it is the BEHAVIOUR that a flag turns off, never the registration.
/// </remarks>
public sealed class UnlimitedQuotaService : IQuotaService
{
    /// <inheritdoc />
    public Task<QuotaUsage> CheckAndRecordAsync(
        string tenantId,
        QuotaResource resource,
        long units = 1,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unlimited(resource));

    /// <inheritdoc />
    public Task<QuotaUsage> GetUsageAsync(
        string tenantId,
        QuotaResource resource,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unlimited(resource));

    /// <inheritdoc />
    public Task<IReadOnlyCollection<QuotaUsage>> GetAllUsageAsync(
        string tenantId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<QuotaUsage>>(
            [.. Enum.GetValues<QuotaResource>().Select(Unlimited)]);

    private static QuotaUsage Unlimited(QuotaResource resource) => new(resource, 0, 0, null);
}
