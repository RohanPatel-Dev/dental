using Dental.Framework.Core.Domain;

namespace Dental.Framework.Eventing.Inbox;

/// <summary>
/// Record that one handler has already processed one event. The composite key
/// (<see cref="EventId"/>, <see cref="HandlerName"/>) is what makes redelivery idempotent.
/// </summary>
/// <remarks>
/// Global by design: the consumer resolves handlers in a fresh scope with no ambient tenant.
/// </remarks>
public sealed class InboxMessage : BaseEntity, IGlobalEntity
{
    /// <summary>Identifier of the integration event that was handled.</summary>
    public Guid EventId { get; set; }

    /// <summary>Full name of the handler type that processed it.</summary>
    public string HandlerName { get; set; } = default!;

    /// <summary>Assembly qualified name of the event type, kept for diagnostics.</summary>
    public string EventType { get; set; } = default!;

    /// <summary>When processing completed.</summary>
    public DateTimeOffset ProcessedOnUtc { get; set; }
}
