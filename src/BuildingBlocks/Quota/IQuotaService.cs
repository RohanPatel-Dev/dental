namespace Dental.Framework.Quota;

/// <summary>Tracks and enforces per-tenant resource consumption.</summary>
public interface IQuotaService
{
    /// <summary>
    /// Atomically checks a limit and, if there is room, records the consumption.
    /// </summary>
    /// <param name="tenantId">Tenant to charge.</param>
    /// <param name="resource">Resource being consumed.</param>
    /// <param name="units">Units to charge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Usage after the attempt; <see cref="QuotaUsage.IsExceeded"/> means it was refused.</returns>
    /// <remarks>
    /// Check and record are one operation on purpose. Splitting them lets two concurrent requests
    /// both observe room and both consume the last unit.
    /// </remarks>
    Task<QuotaUsage> CheckAndRecordAsync(
        string tenantId,
        QuotaResource resource,
        long units = 1,
        CancellationToken cancellationToken = default);

    /// <summary>Reads current usage without charging anything.</summary>
    /// <param name="tenantId">Tenant to inspect.</param>
    /// <param name="resource">Resource to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current usage.</returns>
    Task<QuotaUsage> GetUsageAsync(
        string tenantId,
        QuotaResource resource,
        CancellationToken cancellationToken = default);

    /// <summary>Reads usage for every resource.</summary>
    /// <param name="tenantId">Tenant to inspect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Usage per resource.</returns>
    Task<IReadOnlyCollection<QuotaUsage>> GetAllUsageAsync(
        string tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>Supplies the current level of a gauge resource, e.g. bytes stored or users created.</summary>
public interface IQuotaGaugeProvider
{
    /// <summary>The resource this provider measures.</summary>
    QuotaResource Resource { get; }

    /// <summary>Reads the current level.</summary>
    /// <param name="tenantId">Tenant to measure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current level in the resource's units.</returns>
    Task<long> GetCurrentAsync(string tenantId, CancellationToken cancellationToken = default);
}

/// <summary>Supplies per-tenant limit overrides, normally derived from the tenant's plan.</summary>
public interface IQuotaLimitProvider
{
    /// <summary>Reads the limit for one resource.</summary>
    /// <param name="tenantId">Tenant to look up.</param>
    /// <param name="resource">Resource to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The limit, or null to fall back to the configured default.</returns>
    Task<long?> GetLimitAsync(
        string tenantId,
        QuotaResource resource,
        CancellationToken cancellationToken = default);
}
