using Dental.Framework.Core.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dental.Framework.Persistence.Interceptors;

/// <summary>
/// Publishes queued domain events after the unit of work has been saved, then clears the queues.
/// </summary>
/// <remarks>
/// Dispatch happens after <c>SaveChanges</c> succeeds, so handlers observe persisted state. Domain
/// events are in-process only; anything that must cross a module or a process boundary belongs in
/// the outbox as an integration event.
/// </remarks>
/// <param name="publisher">Mediator publisher.</param>
public sealed class DomainEventDispatchInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        int saved = await base.SavedChangesAsync(eventData, result, cancellationToken)
            .ConfigureAwait(false);

        foreach (DomainEvent domainEvent in Drain(eventData.Context))
        {
            await publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
        }

        return saved;
    }

    private static List<DomainEvent> Drain(DbContext? context)
    {
        if (context is null)
        {
            return [];
        }

        List<EntityEntry<IHasDomainEvents>> aggregates =
        [
            .. context.ChangeTracker.Entries<IHasDomainEvents>()
                .Where(e => e.Entity.DomainEvents.Count > 0),
        ];

        List<DomainEvent> events = [.. aggregates.SelectMany(a => a.Entity.DomainEvents)];

        foreach (EntityEntry<IHasDomainEvents> aggregate in aggregates)
        {
            aggregate.Entity.ClearDomainEvents();
        }

        return events;
    }
}
