using System.Globalization;
using System.Security.Claims;
using Dental.Framework.Shared.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Channels;

namespace Dental.Framework.Web.Realtime;

/// <summary>Two-step token handshake plus the streaming endpoint.</summary>
public static class SseEndpoints
{
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);

    /// <summary>Maps the token exchange and the event stream.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapServerSentEvents(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost(ApiRoutes.SseToken, (HttpContext context, SseTokenStore tokens) =>
            {
                if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId))
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(new { token = tokens.Issue(userId) });
            })
            .WithName("IssueSseToken")
            .WithSummary("Issue a single-use server-sent-events token")
            .WithTags("Realtime")
            .RequireAuthorization();

        endpoints.MapGet(ApiRoutes.SseStream, StreamAsync)
            .WithName("StreamServerSentEvents")
            .WithSummary("Stream server-sent events")
            .WithTags("Realtime")
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task StreamAsync(
        HttpContext context,
        SseTokenStore tokens,
        SseConnectionManager connections,
        TimeProvider timeProvider,
        Guid token)
    {
        if (!tokens.TryConsume(token, out Guid userId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        // Without this, nginx buffers the whole stream and the client receives nothing until close.
        context.Response.Headers[HeaderNames.AccelBuffering] = "no";

        Guid connectionId = Guid.CreateVersion7();
        ChannelReader<SseMessage> reader = connections.Subscribe(userId, connectionId);

        try
        {
            using PeriodicTimer heartbeat = new(HeartbeatInterval, timeProvider);
            Task<bool> heartbeatTask = heartbeat.WaitForNextTickAsync(context.RequestAborted).AsTask();
            Task<bool> readTask = reader.WaitToReadAsync(context.RequestAborted).AsTask();

            while (!context.RequestAborted.IsCancellationRequested)
            {
                Task completed = await Task.WhenAny(readTask, heartbeatTask).ConfigureAwait(false);

                if (completed == heartbeatTask)
                {
                    await context.Response.WriteAsync(": heartbeat\n\n", context.RequestAborted)
                        .ConfigureAwait(false);
                    await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);
                    heartbeatTask = heartbeat.WaitForNextTickAsync(context.RequestAborted).AsTask();
                    continue;
                }

                if (!await readTask.ConfigureAwait(false))
                {
                    break;
                }

                while (reader.TryRead(out SseMessage? message))
                {
                    await WriteEventAsync(context, message).ConfigureAwait(false);
                }

                readTask = reader.WaitToReadAsync(context.RequestAborted).AsTask();
            }
        }
        catch (OperationCanceledException)
        {
            // The client disconnected. Normal, not an error.
        }
        finally
        {
            connections.Unsubscribe(userId, connectionId);
        }
    }

    private static async Task WriteEventAsync(HttpContext context, SseMessage message)
    {
        string payload = string.Create(
            CultureInfo.InvariantCulture,
            $"event: {message.EventName}\ndata: {message.Data}\n\n");

        await context.Response.WriteAsync(payload, context.RequestAborted).ConfigureAwait(false);
        await context.Response.Body.FlushAsync(context.RequestAborted).ConfigureAwait(false);
    }
}
