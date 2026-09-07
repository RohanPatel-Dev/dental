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

        // Staged only. The handler's own SaveChanges commits this row alongside its work, so a
        // handler that throws leaves no claim behind and the redelivery retries it.
        set.Add(new InboxMessage
        {
            EventId = eventId,
            HandlerName = handlerName,
            EventType = eventType,
            ProcessedOnUtc = timeProvider.GetUtcNow(),
            TenantId = string.Empty,
        });

        return true;
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (!context.ChangeTracker.HasChanges())
        {
            return;
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception)
        {
            // Two deliveries raced on the same event and handler. The unique index rejected the
            // loser; the work was done twice at most once, which is what the inbox exists to bound.
            logger.LogDebug(
                exception,
                "Inbox claim lost a race and was rejected by the unique index.");
        }
    }
}
