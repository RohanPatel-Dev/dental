using Microsoft.AspNetCore.SignalR;

namespace Dental.Framework.Web.Realtime;

/// <summary>Pushes realtime messages without a module having to know about SignalR.</summary>
public interface IRealtimeNotifier
{
    /// <summary>Sends to every connection of one user.</summary>
    /// <typeparam name="TPayload">Payload type.</typeparam>
    /// <param name="userId">Target user.</param>
    /// <param name="eventName">Client side event name.</param>
    /// <param name="payload">Payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the message has been queued.</returns>
    Task SendToUserAsync<TPayload>(
        Guid userId,
        string eventName,
        TPayload payload,
        CancellationToken cancellationToken = default);

    /// <summary>Sends to every connection in one tenant.</summary>
    /// <typeparam name="TPayload">Payload type.</typeparam>
    /// <param name="tenantId">Target tenant.</param>
    /// <param name="eventName">Client side event name.</param>
    /// <param name="payload">Payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the message has been queued.</returns>
    Task SendToTenantAsync<TPayload>(
        string tenantId,
        string eventName,
        TPayload payload,
        CancellationToken cancellationToken = default);

    /// <summary>Sends to a channel group.</summary>
    /// <typeparam name="TPayload">Payload type.</typeparam>
    /// <param name="tenantId">Tenant owning the channel.</param>
    /// <param name="channelId">Target channel.</param>
    /// <param name="eventName">Client side event name.</param>
    /// <param name="payload">Payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the message has been queued.</returns>
    Task SendToChannelAsync<TPayload>(
        string tenantId,
        string channelId,
        string eventName,
        TPayload payload,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// SignalR backed notifier.
/// </summary>
/// <remarks>
/// A headless host can use this without ever calling <c>MapHub</c>: with SignalR services registered
/// and a shared Redis backplane, <see cref="IHubContext{THub}"/> reaches clients connected to other
/// processes. If Redis is unset or points elsewhere the push silently no-ops.
/// </remarks>
/// <param name="hubContext">Hub context.</param>
public sealed class RealtimeNotifier(IHubContext<AppHub> hubContext) : IRealtimeNotifier
{
    /// <inheritdoc />
    public Task SendToUserAsync<TPayload>(
        Guid userId,
        string eventName,
        TPayload payload,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(AppHub.UserGroup(userId))
            .SendAsync(eventName, payload, cancellationToken);

    /// <inheritdoc />
    public Task SendToTenantAsync<TPayload>(
        string tenantId,
        string eventName,
        TPayload payload,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(AppHub.TenantGroup(tenantId))
            .SendAsync(eventName, payload, cancellationToken);

    /// <inheritdoc />
    public Task SendToChannelAsync<TPayload>(
        string tenantId,
        string channelId,
        string eventName,
        TPayload payload,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Group(AppHub.ChannelGroup($"{tenantId}:{channelId}"))
            .SendAsync(eventName, payload, cancellationToken);
}
