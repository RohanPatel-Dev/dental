namespace Dental.Framework.Quota;

/// <summary>
/// Fallback limit provider used until a module (normally Tenancy, from the tenant's plan) supplies
/// a real one. Always defers to the configured defaults.
/// </summary>
public sealed class NoQuotaLimitProvider : IQuotaLimitProvider
{
    /// <inheritdoc />
    public Task<long?> GetLimitAsync(
        string tenantId,
        QuotaResource resource,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<long?>(null);
}
