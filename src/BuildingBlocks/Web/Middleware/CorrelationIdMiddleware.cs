using System.Diagnostics;
using Dental.Framework.Shared.Http;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Dental.Framework.Web.Middleware;

/// <summary>
/// Establishes a correlation identifier for the request, echoes it back, pushes it into the Serilog
/// log context and tags the current activity with it.
/// </summary>
/// <param name="next">Next middleware.</param>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>Runs the middleware.</summary>
    /// <param name="context">The request.</param>
    /// <returns>A task that completes when the pipeline has run.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? incoming = context.Request.Headers[HeaderNames.CorrelationId];
        string correlationId = string.IsNullOrWhiteSpace(incoming) ? context.TraceIdentifier : incoming;

        context.Response.Headers[HeaderNames.CorrelationId] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
