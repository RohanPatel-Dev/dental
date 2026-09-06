namespace Dental.Framework.Core.Contracts;

/// <summary>
/// Re-establishes the ambient tenant inside a scope that has no HTTP request behind it.
/// </summary>
/// <remarks>
/// Background work - Hangfire jobs, outbox dispatch, integration event handlers - runs with no HTTP
/// context and therefore no resolved tenant. Touching a tenant filtered <c>DbContext</c> in that
/// state throws inside the query filter. Implementations live next to the tenant store, because
/// only that layer knows the concrete tenant info type Finbuckle was configured with.
/// </remarks>
public interface ITenantContextRestorer
{
    /// <summary>Sets the ambient tenant for the current asynchronous flow.</summary>
    /// <param name="tenantId">Identifier of the tenant to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes once the context is set.</returns>
    Task RestoreAsync(string tenantId, CancellationToken cancellationToken = default);
}
