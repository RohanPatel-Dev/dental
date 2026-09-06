using System.Security.Claims;
using Dental.Framework.Shared.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Dental.Framework.Web.Realtime;

/// <summary>
/// The single SignalR hub. Broadcast to groups, never <c>Clients.All</c> - a tenant must never see
/// another tenant's traffic.
/// </summary>
/// <remarks>
/// Inside a hub the caller is read from <see cref="HubCallerContext.User"/>, NOT from
/// <c>ICurrentUser</c>: the negotiate <c>HttpContext</c> is not pinned to subsequent hub
/// invocations, so the accessor returns nulls.
/// </remarks>
/// <param name="logger">Logger.</param>
[Authorize]
public sealed class AppHub(ILogger<AppHub> logger) : Hub
{
    /// <summary>Group carrying everything addressed to one user.</summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>The group name.</returns>
    public static string UserGroup(Guid userId) => $"user:{userId}";

    /// <summary>Group carrying everything addressed to one tenant.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <returns>The group name.</returns>
    public static string TenantGroup(string tenantId) => $"tenant:{tenantId}";

    /// <summary>Group carrying one topic, e.g. a single operatory's schedule.</summary>
    /// <param name="channelId">Channel identifier.</param>
    /// <returns>The group name.</returns>
    public static string ChannelGroup(string channelId) => $"channel:{channelId}";

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        ClaimsPrincipal? user = Context.User;

        if (Guid.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId)).ConfigureAwait(false);
        }

        string? tenantId = user?.FindFirstValue(DentalClaims.Tenant);
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId))
                .ConfigureAwait(false);
        }

        logger.LogDebug("Realtime connection {ConnectionId} joined its groups.", Context.ConnectionId);

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Joins a channel group that became relevant after the socket opened.
    /// </summary>
    /// <param name="channelId">Channel to join.</param>
    /// <returns>A task that completes when the connection has joined.</returns>
    /// <remarks>
    /// Groups are only assigned at connect time. Without this method a client that gains access to
    /// a channel mid-session silently misses every broadcast until it reloads the page.
    /// </remarks>
    public async Task JoinChannel(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);

        string? tenantId = Context.User?.FindFirstValue(DentalClaims.Tenant);
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new HubException("A tenant is required to join a channel.");
        }

        // Channel names are namespaced by tenant so membership cannot be forged across tenants.
        await Groups.AddToGroupAsync(Context.ConnectionId, ChannelGroup($"{tenantId}:{channelId}"))
            .ConfigureAwait(false);
    }

    /// <summary>Leaves a channel group.</summary>
    /// <param name="channelId">Channel to leave.</param>
    /// <returns>A task that completes when the connection has left.</returns>
    public async Task LeaveChannel(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);

        string? tenantId = Context.User?.FindFirstValue(DentalClaims.Tenant);
        if (string.IsNullOrEmpty(tenantId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ChannelGroup($"{tenantId}:{channelId}"))
            .ConfigureAwait(false);
    }
}
