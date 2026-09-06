namespace Dental.Framework.Eventing.Abstractions;

/// <summary>
/// A fact published across a module or process boundary. Always published through the outbox, never
/// directly on the bus.
/// </summary>
/// <remarks>
/// The outbox stores the assembly qualified type name of the concrete event. Renaming or moving an
/// event type therefore breaks deserialization of rows already queued - keep names and namespaces
/// stable, and introduce a new type instead of renaming an old one.
/// </remarks>
public interface IIntegrationEvent
{
    /// <summary>Unique identifier of this occurrence. Doubles as the inbox deduplication key.</summary>
    Guid Id { get; }

    /// <summary>When the fact happened, in UTC.</summary>
    DateTimeOffset OccurredOnUtc { get; }

    /// <summary>
    /// Tenant the fact belongs to. Carried explicitly because the dispatcher runs with no ambient
    /// tenant context.
    /// </summary>
    string? TenantId { get; }

    /// <summary>Correlation identifier flowing from the originating request.</summary>
    string? CorrelationId { get; }

    /// <summary>Module that published the event.</summary>
    string Source { get; }
}
