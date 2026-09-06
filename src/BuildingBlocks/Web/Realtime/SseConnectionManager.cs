using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Dental.Framework.Web.Realtime;

/// <summary>
/// Holds one bounded channel per live server-sent-events connection.
/// </summary>
/// <remarks>
/// Channels are bounded at 100 with <see cref="BoundedChannelFullMode.DropOldest"/>: a browser tab
/// that stops reading must never grow the server's heap. Losing the oldest notification is a better
/// failure than running out of memory.
/// </remarks>
public sealed class SseConnectionManager
{
    private const int Capacity = 100;

    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<SseMessage>>> _connections =
        new();

    /// <summary>Opens a channel for a new connection.</summary>
    /// <param name="userId">Owning user.</param>
    /// <param name="connectionId">Identifier for this connection.</param>
    /// <returns>The reader the streaming endpoint drains.</returns>
    public ChannelReader<SseMessage> Subscribe(Guid userId, Guid connectionId)
    {
        Channel<SseMessage> channel = Channel.CreateBounded<SseMessage>(
            new BoundedChannelOptions(Capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            });

        _connections.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Channel<SseMessage>>())
            [connectionId] = channel;

        return channel.Reader;
    }

    /// <summary>Closes and forgets a connection.</summary>
    /// <param name="userId">Owning user.</param>
    /// <param name="connectionId">Connection to drop.</param>
    public void Unsubscribe(Guid userId, Guid connectionId)
    {
        if (!_connections.TryGetValue(userId, out ConcurrentDictionary<Guid, Channel<SseMessage>>? channels))
        {
            return;
        }

        if (channels.TryRemove(connectionId, out Channel<SseMessage>? channel))
        {
            channel.Writer.TryComplete();
        }

        if (channels.IsEmpty)
        {
            _connections.TryRemove(userId, out _);
        }
    }

    /// <summary>Publishes a message to every connection of one user.</summary>
    /// <param name="userId">Target user.</param>
    /// <param name="message">The message.</param>
    public void Publish(Guid userId, SseMessage message)
    {
        if (!_connections.TryGetValue(userId, out ConcurrentDictionary<Guid, Channel<SseMessage>>? channels))
        {
            return;
        }

        foreach (Channel<SseMessage> channel in channels.Values)
        {
            channel.Writer.TryWrite(message);
        }
    }
}

/// <summary>One server-sent event.</summary>
/// <param name="EventName">The <c>event:</c> field.</param>
/// <param name="Data">The <c>data:</c> payload, already serialized.</param>
public sealed record SseMessage(string EventName, string Data);
