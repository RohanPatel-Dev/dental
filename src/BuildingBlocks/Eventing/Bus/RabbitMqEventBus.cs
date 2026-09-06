using System.Text;
using Dental.Framework.Eventing.Abstractions;
using Dental.Framework.Eventing.Serialization;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Dental.Framework.Eventing.Bus;

/// <summary>
/// Publishes integration events to a durable topic exchange.
/// </summary>
/// <remarks>
/// The routing key is the stored (assembly qualified) event type name, which is why renaming an
/// event type also breaks in-flight delivery, not just outbox rows.
/// </remarks>
/// <param name="connectionProvider">Shared RabbitMQ connection.</param>
/// <param name="options">Eventing options.</param>
public sealed class RabbitMqEventBus(
    RabbitMqConnectionProvider connectionProvider,
    IOptions<EventingOptions> options) : IEventBus
{
    private readonly EventingOptions _options = options.Value;

    /// <inheritdoc />
    public async Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        IChannel channel = await connectionProvider.CreateChannelAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (channel.ConfigureAwait(false))
        {
            await channel.ExchangeDeclareAsync(
                    _options.ExchangeName,
                    ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            string routingKey = IntegrationEventSerializer.GetTypeName(integrationEvent.GetType());
            byte[] body = Encoding.UTF8.GetBytes(IntegrationEventSerializer.Serialize(integrationEvent));

            BasicProperties properties = new()
            {
                Persistent = true,
                MessageId = integrationEvent.Id.ToString(),
                CorrelationId = integrationEvent.CorrelationId,
                Type = routingKey,
                ContentType = "application/json",
                Headers = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["x-tenant-id"] = integrationEvent.TenantId,
                    ["x-retry-count"] = 0,
                },
            };

            await channel.BasicPublishAsync(
                    _options.ExchangeName,
                    routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
