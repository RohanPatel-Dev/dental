using Microsoft.AspNetCore.Builder;

namespace Dental.Framework.Web.Middleware;

/// <summary>Opts an endpoint into idempotent replay.</summary>
public static class IdempotencyEndpointExtensions
{
    /// <summary>
    /// Marks a POST as replay safe: a repeated <c>Idempotency-Key</c> returns the recorded response
    /// with <c>Idempotency-Replayed: true</c> instead of performing the work twice.
    /// </summary>
    /// <param name="builder">The endpoint builder.</param>
    /// <returns>The endpoint builder, for chaining.</returns>
    public static RouteHandlerBuilder WithIdempotency(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithMetadata(new IdempotencyMetadata());
    }
}
