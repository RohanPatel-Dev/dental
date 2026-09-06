namespace Dental.Framework.Eventing.Abstractions;

/// <summary>Marker for the open generic handler interface, used for DI scanning.</summary>
public interface IIntegrationEventHandler;

/// <summary>
/// Handles one integration event type. Implementations live in the module's <c>Events/</c> folder
/// and are <see langword="sealed"/>.
/// </summary>
/// <typeparam name="TEvent">The event type handled.</typeparam>
/// <remarks>
/// Handlers are resolved in a fresh DI scope with no HTTP context and no ambient tenant. Restore
/// the Finbuckle tenant context from <see cref="IIntegrationEvent.TenantId"/> before touching a
/// tenant filtered <c>DbContext</c>. Deduplication is handled by the inbox - do not hand roll it.
/// </remarks>
public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler
    where TEvent : IIntegrationEvent
{
    /// <summary>Handles the event.</summary>
    /// <param name="integrationEvent">The event instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when handling is done.</returns>
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
