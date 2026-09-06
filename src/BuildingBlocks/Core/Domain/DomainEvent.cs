using Mediator;

namespace Dental.Framework.Core.Domain;

/// <summary>
/// In-process, pre-commit notification raised by an aggregate. Dispatched by an EF interceptor
/// inside the same transaction. For anything crossing a module or process boundary use an
/// integration event and the outbox instead.
/// </summary>
/// <remarks>
/// Domain events are Mediator notifications, so a handler is just
/// <c>INotificationHandler&lt;MyDomainEvent&gt;</c> living inside the owning module.
/// </remarks>
public abstract record DomainEvent : INotification
{
    /// <summary>Unique identifier of this occurrence.</summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    /// <summary>When the event happened, in UTC.</summary>
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Correlation identifier flowing from the originating request.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Tenant the event belongs to.</summary>
    public string? TenantId { get; init; }
}
