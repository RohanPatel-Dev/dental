using Dental.Framework.Web.Auth;
using Dental.Modules.Identity.Contracts.Authorization;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Users.GetUser;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Identity.Features.v1.Users.GetUser;

/// <summary>Maps <c>GET /users/{id:guid}</c>.</summary>
public static class GetUserEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapGetUserEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/users/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(new GetUserQuery(id), cancellationToken))
            .WithName("GetUser")
            .WithSummary("Read one staff account")
            .Produces<UserDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(IdentityPermissions.Users.View);
}
