using Dental.Framework.Eventing.Abstractions;

namespace Dental.Framework.Eventing.Bus;

/// <summary>
/// Dispatches integration events synchronously inside the publishing process.
/// </summary>
/// <remarks>
/// Handler work happens on the dispatcher's thread and handler exceptions surface to the caller.
/// This bus CANNOT cross a process boundary - an extracted host requires RabbitMQ.
/// </remarks>
/// <param name="dispatcher">The shared handler dispatcher.</param>
public sealed class InMemoryEventBus(IntegrationEventDispatcher dispatcher) : IEventBus
{
    /// <inheritdoc />
    public Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default) =>
        dispatcher.DispatchAsync(integrationEvent, cancellationToken);
}
