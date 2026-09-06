using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Account.GetCurrentUser;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Identity.Features.v1.Account.GetCurrentUser;

/// <summary>Maps <c>GET /account/me</c>.</summary>
public static class GetCurrentUserEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapGetCurrentUserEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/account/me",
                (IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(new GetCurrentUserQuery(), cancellationToken))
            .WithName("GetCurrentUser")
            .WithSummary("Read the signed-in user's profile and permissions")
            .Produces<CurrentUserDto>(StatusCodes.Status200OK)
            .RequireAuthorization();
}
