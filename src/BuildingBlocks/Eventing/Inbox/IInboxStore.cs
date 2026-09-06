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
    /// Records that a handler processed an event, returning false when it already had.
    /// </summary>
    /// <param name="eventId">Identifier of the integration event.</param>
    /// <param name="handlerName">Full name of the handler type.</param>
    /// <param name="eventType">Stored type name of the event, for diagnostics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> when this call claimed the work, <see langword="false"/> when another
    /// delivery already did.
    /// </returns>
    Task<bool> TryClaimAsync(
        Guid eventId,
        string handlerName,
        string eventType,
        CancellationToken cancellationToken = default);
}
