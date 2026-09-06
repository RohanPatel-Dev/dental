using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Framework.Eventing.Inbox;

/// <summary>Inbox backed by one module <c>DbContext</c>.</summary>
/// <typeparam name="TContext">The module context that owns the inbox table.</typeparam>
/// <param name="context">The module context.</param>
/// <param name="timeProvider">Clock.</param>
/// <param name="logger">Logger.</param>
public sealed class InboxStore<TContext>(
    TContext context,
    TimeProvider timeProvider,
    ILogger<InboxStore<TContext>> logger) : IInboxStore
    where TContext : DbContext
{
    /// <inheritdoc />
    public string OwnerAssembly { get; } = typeof(TContext).Assembly.GetName().Name!;

    /// <inheritdoc />
    public async Task<bool> TryClaimAsync(
        Guid eventId,
        string handlerName,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        DbSet<InboxMessage> set = context.Set<InboxMessage>();

        bool alreadyHandled = await set
            .AsNoTracking()
            .AnyAsync(m => m.EventId == eventId && m.HandlerName == handlerName, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyHandled)
        {
            return false;
        }

        set.Add(new InboxMessage
        {
            EventId = eventId,
            HandlerName = handlerName,
            EventType = eventType,
            ProcessedOnUtc = timeProvider.GetUtcNow(),
            TenantId = string.Empty,
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException exception)
        {
            // Two deliveries raced on the same event and handler. The unique index rejected the
            // loser, which then simply skips the handler.
            logger.LogDebug(
                exception,
                "Inbox claim lost a race for event {EventId} and handler {HandlerName}.",
                eventId,
                handlerName);

            context.Entry(set.Local.First(m => m.EventId == eventId && m.HandlerName == handlerName))
                .State = EntityState.Detached;

            return false;
        }
    }
}
