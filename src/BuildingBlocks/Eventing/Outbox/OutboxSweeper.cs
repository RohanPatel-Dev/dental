using Dental.Framework.Eventing.Abstractions;
using Dental.Framework.Eventing.Bus;
using Dental.Framework.Eventing.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dental.Framework.Eventing.Outbox;

/// <summary>Drains the outbox table owned by one module <c>DbContext</c>.</summary>
/// <typeparam name="TContext">The module context.</typeparam>
/// <param name="context">The module context.</param>
/// <param name="eventBus">Transport to publish on.</param>
/// <param name="options">Eventing options.</param>
/// <param name="timeProvider">Clock.</param>
/// <param name="logger">Logger.</param>
public sealed class OutboxSweeper<TContext>(
    TContext context,
    IEventBus eventBus,
    IOptions<EventingOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxSweeper<TContext>> logger) : IOutboxSweeper
    where TContext : DbContext
{
    private readonly EventingOptions _options = options.Value;

    /// <inheritdoc />
    public string OwnerAssembly { get; } = typeof(TContext).Assembly.GetName().Name!;

    /// <inheritdoc />
    public async Task<int> SweepAsync(CancellationToken cancellationToken = default)
    {
        List<OutboxMessage> batch = await context.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null && !m.IsDeadLettered)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_options.OutboxBatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (batch.Count == 0)
        {
            return 0;
        }

        int published = 0;

        foreach (OutboxMessage message in batch)
        {
            if (await TryPublishAsync(message, cancellationToken).ConfigureAwait(false))
            {
                published++;
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return published;
    }

    private async Task<bool> TryPublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        Type? eventType = IntegrationEventSerializer.ResolveType(message.EventType);
        if (eventType is null)
        {
            // The type was renamed, moved or removed. There is no way to deserialize this row, so
            // dead letter it immediately rather than retrying forever.
            message.IsDeadLettered = true;
            message.Error = $"Event type '{message.EventType}' could not be resolved.";
            logger.LogError(
                "Dead lettering outbox message {MessageId}: type {EventType} no longer exists.",
                message.Id,
                message.EventType);
            return false;
        }

        try
        {
            IIntegrationEvent integrationEvent =
                IntegrationEventSerializer.Deserialize(message.Payload, eventType);

            await eventBus.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

            message.ProcessedOnUtc = timeProvider.GetUtcNow();
            message.Error = null;
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            message.Attempts++;
            message.Error = exception.Message;

            if (message.Attempts >= _options.OutboxMaxRetries)
            {
                message.IsDeadLettered = true;
                logger.LogError(
                    exception,
                    "Dead lettering outbox message {MessageId} after {Attempts} attempts.",
                    message.Id,
                    message.Attempts);
            }
            else
            {
                logger.LogWarning(
                    exception,
                    "Outbox message {MessageId} failed on attempt {Attempts}; will retry.",
                    message.Id,
                    message.Attempts);
            }

            return false;
        }
    }
}
