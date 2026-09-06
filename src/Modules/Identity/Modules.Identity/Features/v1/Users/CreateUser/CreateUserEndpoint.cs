using Dental.Framework.Web.Auth;
using Dental.Framework.Web.Middleware;
using Dental.Modules.Identity.Contracts.Authorization;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Users.CreateUser;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Identity.Features.v1.Users.CreateUser;

/// <summary>Maps <c>POST /users</c>.</summary>
public static class CreateUserEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapCreateUserEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/users",
                (CreateUserCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(command, cancellationToken))
            .WithName("CreateUser")
            .WithSummary("Create a staff account")
            .Produces<UserDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequirePermission(IdentityPermissions.Users.Create)
            .WithIdempotency();
}
