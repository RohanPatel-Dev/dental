namespace Dental.Framework.Eventing.Abstractions;

/// <summary>
/// Declares that an event type exists even though no handler in this process consumes it.
/// </summary>
/// <remarks>
/// The RabbitMQ consumer declares one queue per known event type. An event with no in-process
/// handler and no marker gets no queue, so it is published into the void. Register one from the
/// publishing module: <c>services.AddSingleton(new IntegrationEventTypeMarker(typeof(XEvent)));</c>.
/// </remarks>
/// <param name="EventType">The concrete event type.</param>
public sealed record IntegrationEventTypeMarker(Type EventType);
