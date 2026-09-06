using System.Text;
using Dental.Framework.Eventing.Abstractions;
using Dental.Framework.Eventing.Bus;
using Dental.Framework.Eventing.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Dental.Framework.Eventing.Hosting;

/// <summary>
/// Binds one durable queue per (event type, consuming assembly) and feeds deliveries to the shared
/// dispatcher.
/// </summary>
/// <remarks>
/// Keying the queue by the CONSUMER's assembly gives competing-consumers semantics for free when the
/// same module is loaded in two processes: both instances share one queue and each message is
/// handled once. Failed handlers republish with an incremented <c>x-retry-count</c> and move to a
/// per-queue <c>.dlq</c> once the limit is reached.
/// </remarks>
/// <param name="connectionProvider">Shared RabbitMQ connection.</param>
/// <param name="dispatcher">The shared handler dispatcher.</param>
/// <param name="registry">Known event types, from handlers and explicit markers.</param>
/// <param name="options">Eventing options.</param>
/// <param name="logger">Logger.</param>
public sealed class RabbitMqConsumerHostedService(
    RabbitMqConnectionProvider connectionProvider,
    IntegrationEventDispatcher dispatcher,
    IntegrationEventTypeRegistry registry,
    IOptions<EventingOptions> options,
    ILogger<RabbitMqConsumerHostedService> logger) : BackgroundService
{
    private readonly EventingOptions _options = options.Value;
    private readonly List<IChannel> _channels = [];

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (Type eventType in registry.EventTypes)
        {
            await BindQueueAsync(eventType, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation(
            "RabbitMQ consumer bound {QueueCount} queue(s) on exchange {Exchange}.",
            _channels.Count,
            _options.ExchangeName);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (IChannel channel in _channels)
        {
            await channel.CloseAsync(cancellationToken).ConfigureAwait(false);
            await channel.DisposeAsync().ConfigureAwait(false);
        }

        _channels.Clear();
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task BindQueueAsync(Type eventType, CancellationToken cancellationToken)
    {
        string routingKey = IntegrationEventSerializer.GetTypeName(eventType);
        string queueName = $"{registry.ConsumerName}.{eventType.Name}";
        string deadLetterQueue = $"{queueName}.dlq";

        IChannel channel = await connectionProvider.CreateChannelAsync(cancellationToken)
            .ConfigureAwait(false);
        _channels.Add(channel);

        await channel.ExchangeDeclareAsync(
                _options.ExchangeName,
                ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await channel.QueueDeclareAsync(
                deadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await channel.QueueDeclareAsync(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        await channel.QueueBindAsync(
                queueName,
                _options.ExchangeName,
                routingKey,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += (_, args) =>
            HandleDeliveryAsync(channel, queueName, deadLetterQueue, args, cancellationToken);

        await channel.BasicConsumeAsync(
                queueName,
                autoAck: false,
                consumer,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        string queueName,
        string deadLetterQueue,
        BasicDeliverEventArgs args,
        CancellationToken cancellationToken)
    {
        string payload = Encoding.UTF8.GetString(args.Body.Span);
        Type? eventType = IntegrationEventSerializer.ResolveType(args.BasicProperties.Type ?? string.Empty);

        if (eventType is null)
        {
            logger.LogError(
                "Cannot resolve event type {TypeName} from queue {Queue}; dead lettering.",
                args.BasicProperties.Type,
                queueName);
            await PublishToDeadLetterAsync(channel, deadLetterQueue, args, cancellationToken)
                .ConfigureAwait(false);
            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        try
        {
            IIntegrationEvent integrationEvent =
                IntegrationEventSerializer.Deserialize(payload, eventType);

            await dispatcher.DispatchAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            int retryCount = ReadRetryCount(args) + 1;

            logger.LogError(
                exception,
                "Handling failed for {EventType} on queue {Queue}, retry {RetryCount}.",
                eventType.Name,
                queueName,
                retryCount);

            if (retryCount >= _options.ConsumerMaxRetries)
            {
                await PublishToDeadLetterAsync(channel, deadLetterQueue, args, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await RepublishAsync(channel, queueName, args, retryCount, cancellationToken)
                    .ConfigureAwait(false);
            }

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static int ReadRetryCount(BasicDeliverEventArgs args) =>
        args.BasicProperties.Headers?.TryGetValue("x-retry-count", out object? value) == true
        && value is int count
            ? count
            : 0;

    private static async Task RepublishAsync(
        IChannel channel,
        string queueName,
        BasicDeliverEventArgs args,
        int retryCount,
        CancellationToken cancellationToken)
    {
        BasicProperties properties = Clone(args, retryCount);

        await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: args.Body.ToArray(),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task PublishToDeadLetterAsync(
        IChannel channel,
        string deadLetterQueue,
        BasicDeliverEventArgs args,
        CancellationToken cancellationToken)
    {
        BasicProperties properties = Clone(args, ReadRetryCount(args));

        await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: deadLetterQueue,
                mandatory: false,
                basicProperties: properties,
                body: args.Body.ToArray(),
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static BasicProperties Clone(BasicDeliverEventArgs args, int retryCount)
    {
        Dictionary<string, object?> headers = args.BasicProperties.Headers is null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(args.BasicProperties.Headers, StringComparer.Ordinal);

        headers["x-retry-count"] = retryCount;

        return new BasicProperties
        {
            Persistent = true,
            MessageId = args.BasicProperties.MessageId,
            CorrelationId = args.BasicProperties.CorrelationId,
            Type = args.BasicProperties.Type,
            ContentType = args.BasicProperties.ContentType,
            Headers = headers,
        };
    }
}
