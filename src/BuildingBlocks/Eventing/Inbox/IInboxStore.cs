namespace Dental.Framework.Eventing.Inbox;

/// <summary>
/// Deduplication record for one module. One implementation is registered per module
/// <c>DbContext</c>; the dispatcher picks the one whose <see cref="OwnerAssembly"/> matches the
/// handler being invoked, so two modules loaded in one process keep separate inbox tables.
/// </summary>
public interface IInboxStore
{
    /// <summary>Simple name of the assembly whose handlers this store serves.</summary>
    string OwnerAssembly { get; }

    /// <summary>
    /// Stages a record that a handler processed an event, returning false when it already had.
    /// </summary>
    /// <param name="eventId">Identifier of the integration event.</param>
    /// <param name="handlerName">Full name of the handler type.</param>
    /// <param name="eventType">Stored type name of the event, for diagnostics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> when this call claimed the work, <see langword="false"/> when a
    /// previous delivery already completed it.
    /// </returns>
    /// <remarks>
    /// The row is ADDED to the change tracker but NOT saved. Committing the claim before the handler
    /// runs would mean a handler that throws is never retried: the redelivery would see the claim,
    /// skip the work, and the event would be silently lost. Saving it together with the handler's
    /// own work makes the two atomic.
    /// </remarks>
    Task<bool> TryClaimAsync(
        Guid eventId,
        string handlerName,
        string eventType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the staged claim, for a handler that made no changes of its own to save.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes once the claim is durable.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);
}
