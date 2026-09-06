using Dental.Framework.Web.RateLimiting;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Tokens.RefreshToken;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Identity.Features.v1.Tokens.RefreshToken;

/// <summary>Maps <c>POST /tokens/refresh</c>.</summary>
public static class RefreshTokenEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapRefreshTokenEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/tokens/refresh",
                (
                    RefreshTokenCommand command,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(command, cancellationToken))
            .WithName("RefreshToken")
            .WithSummary("Rotate a refresh token")
            .Produces<TokenDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingRegistration.AuthPolicy);
}
