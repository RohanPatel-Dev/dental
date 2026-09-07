namespace Dental.Framework.Core.Contracts;

/// <summary>
/// Re-establishes the ambient tenant inside a scope that has no HTTP request behind it.
/// </summary>
/// <remarks>
/// <para>
/// Background work - Hangfire jobs, outbox dispatch, integration event handlers - runs with no HTTP
/// context and therefore no resolved tenant. Touching a tenant filtered <c>DbContext</c> in that
/// state throws inside the query filter, usually as a bare <see cref="NullReferenceException"/> from
/// a compiled query, which is a miserable thing to diagnose.
/// </para>
/// <para>
/// The two-step shape is NOT an accident. The ambient tenant lives in an <c>AsyncLocal</c>, and a
/// write to an <c>AsyncLocal</c> inside an awaited method does not flow back to its caller - so a
/// single <c>await RestoreAsync(id)</c> would appear to succeed and leave the caller with no tenant
/// at all. <see cref="ResolveAsync"/> does the asynchronous lookup; <see cref="Apply"/> is
/// synchronous and must be called by the code that needs the tenant, so the write happens in that
/// frame and flows into everything it subsequently awaits.
/// </para>
/// </remarks>
public interface ITenantContextRestorer
{
    /// <summary>Looks a tenant up, without touching the ambient context.</summary>
    /// <param name="tenantId">Identifier of the tenant to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant, or <see langword="null"/> when it does not exist.</returns>
    Task<TenantSnapshot?> ResolveAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the ambient tenant. Call this from the frame that needs it, immediately after awaiting
    /// <see cref="ResolveAsync"/>.
    /// </summary>
    /// <param name="snapshot">The tenant to make ambient.</param>
    void Apply(TenantSnapshot snapshot);
}

/// <summary>The tenant facts background work needs, independent of the multitenancy library.</summary>
/// <param name="Id">Stable identifier.</param>
/// <param name="Identifier">Human readable slug.</param>
/// <param name="Name">Practice name.</param>
/// <param name="Plan">Subscription plan.</param>
/// <param name="TimeZone">IANA time zone the practice schedules in.</param>
/// <param name="IsActive">Whether the tenant is live.</param>
/// <param name="ValidUntil">When the subscription lapses, if it does.</param>
public sealed record TenantSnapshot(
    string Id,
    string Identifier,
    string? Name,
    string? Plan,
    string TimeZone,
    bool IsActive,
    DateTimeOffset? ValidUntil);
