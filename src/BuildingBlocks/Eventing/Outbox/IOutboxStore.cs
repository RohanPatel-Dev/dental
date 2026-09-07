using Dental.Framework.Eventing.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dental.Framework.Eventing.Outbox;

/// <summary>
/// The ONLY supported way to publish an integration event. Writes the serialized event into the
/// same <c>DbContext</c> - and therefore the same transaction - as the state change that caused it,
/// so a crash between the two is impossible.
/// </summary>
/// <typeparam name="TContext">
/// The module context whose outbox table the event is written to.
/// </typeparam>
/// <remarks>
/// <para>
/// Never inject <c>IEventBus</c> into a command handler. Doing so publishes outside the transaction
/// and produces phantom events when the transaction later rolls back.
/// </para>
/// <para>
/// The context is a TYPE PARAMETER, not an implementation detail. Every module registers its own
/// outbox store in the same container; a single non-generic interface would resolve to whichever
/// module happened to register last, and a handler's event would be written to another module's
/// change tracker and silently discarded when that context was never saved. Naming the context here
/// makes the wrong thing impossible to express.
/// </para>
/// </remarks>
public interface IOutboxStore<TContext>
    where TContext : DbContext
{
    /// <summary>Queues an event for publication when the current unit of work is saved.</summary>
    /// <param name="integrationEvent">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the row has been added to the change tracker.</returns>
    Task AddAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
