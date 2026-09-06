using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Dental.Framework.Eventing.Bus;

/// <summary>Owns the single long lived RabbitMQ connection for the process.</summary>
/// <param name="options">Eventing options.</param>
public sealed class RabbitMqConnectionProvider(IOptions<EventingOptions> options) : IAsyncDisposable
{
    private readonly EventingOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    /// <summary>Opens a channel on the shared connection, connecting on first use.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new channel. The caller owns and disposes it.</returns>
    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        IConnection connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.CreateChannelAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync().ConfigureAwait(false);
            _connection.Dispose();
            _connection = null;
        }

        _gate.Dispose();
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (string.IsNullOrWhiteSpace(_options.RabbitMq))
            {
                throw new InvalidOperationException(
                    "EventingOptions:RabbitMq must be set when the provider is RabbitMQ.");
            }

            ConnectionFactory factory = new()
            {
                Uri = new Uri(_options.RabbitMq),
                AutomaticRecoveryEnabled = true,
                ClientProvidedName = "dental",
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }
}
