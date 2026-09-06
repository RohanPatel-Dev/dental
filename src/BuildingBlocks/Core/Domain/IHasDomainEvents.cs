namespace Dental.Framework.Core.Domain;

/// <summary>Implemented by aggregates that queue domain events for interceptor dispatch.</summary>
public interface IHasDomainEvents
{
    /// <summary>Events queued since the aggregate was loaded.</summary>
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }

    /// <summary>Clears the queue. Called by the dispatching interceptor once events are handled.</summary>
    void ClearDomainEvents();
}
