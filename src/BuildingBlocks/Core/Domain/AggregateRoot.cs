namespace Dental.Framework.Core.Domain;

/// <summary>
/// A <see cref="BaseEntity"/> that owns a consistency boundary and can raise domain events.
/// Only aggregate roots should be fetched directly by a repository or handler.
/// </summary>
public abstract class AggregateRoot : BaseEntity, IHasDomainEvents
{
    private readonly List<DomainEvent> _domainEvents = [];

    /// <inheritdoc />
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>Queues a domain event for dispatch when the current unit of work is saved.</summary>
    /// <param name="domainEvent">The event to raise.</param>
    protected void RaiseDomainEvent(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
