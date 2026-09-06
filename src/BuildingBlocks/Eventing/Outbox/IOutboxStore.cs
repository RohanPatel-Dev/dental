using Dental.Framework.Eventing.Abstractions;

namespace Dental.Framework.Eventing.Outbox;

/// <summary>
/// The ONLY supported way to publish an integration event. Writes the serialized event into the
/// same <c>DbContext</c> - and therefore the same transaction - as the state change that caused it,
/// so a crash between the two is impossible.
/// </summary>
/// <remarks>
/// Never inject <c>IEventBus</c> into a command handler. Doing so publishes outside the transaction
/// and produces phantom events when the transaction later rolls back.
/// </remarks>
public interface IOutboxStore
{
    /// <summary>Queues an event for publication when the current unit of work is saved.</summary>
    /// <param name="integrationEvent">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the row has been added to the change tracker.</returns>
    Task AddAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
