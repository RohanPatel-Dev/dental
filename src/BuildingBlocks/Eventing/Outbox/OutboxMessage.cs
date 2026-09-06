using Dental.Framework.Core.Domain;

namespace Dental.Framework.Eventing.Outbox;

/// <summary>
/// A serialized integration event waiting to be published, written in the same transaction as the
/// state change that produced it.
/// </summary>
/// <remarks>
/// This is an <see cref="IGlobalEntity"/>: the dispatcher runs with no ambient tenant, so
/// <c>TenantId</c> is an ordinary column rather than something a query filter supplies.
/// </remarks>
public sealed class OutboxMessage : BaseEntity, IGlobalEntity
{
    /// <summary>Assembly qualified name of the concrete event type.</summary>
    /// <remarks>
    /// Renaming or moving an event type makes already-queued rows undeserializable
    /// (<c>Type.GetType()</c> returns null and the row dead letters). Keep names and namespaces stable.
    /// </remarks>
    public string EventType { get; set; } = default!;

    /// <summary>JSON payload of the event.</summary>
    public string Payload { get; set; } = default!;

    /// <summary>Correlation identifier carried from the originating request.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>When the event occurred.</summary>
    public DateTimeOffset OccurredOnUtc { get; set; }

    /// <summary>When the event was successfully published, or null while it is pending.</summary>
    public DateTimeOffset? ProcessedOnUtc { get; set; }

    /// <summary>Publish attempts made so far.</summary>
    public int Attempts { get; set; }

    /// <summary>Error from the most recent failed attempt.</summary>
    public string? Error { get; set; }

    /// <summary>True once the message has exhausted its retries and will not be retried again.</summary>
    public bool IsDeadLettered { get; set; }
}
