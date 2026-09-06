using Dental.Framework.Eventing.Abstractions;

namespace Dental.Framework.Eventing.Bus;

/// <summary>
/// Transport for integration events. Only the outbox dispatcher calls this - feature code publishes
/// through <see cref="Outbox.IOutboxStore"/>.
/// </summary>
public interface IEventBus
{
    /// <summary>Publishes one event on the transport.</summary>
    /// <param name="integrationEvent">The event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the transport has accepted the event.</returns>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
