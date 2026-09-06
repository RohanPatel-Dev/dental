using Dental.Framework.Web.Auth;
using Dental.Modules.Identity.Contracts.Authorization;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Roles.CreateRole;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Identity.Features.v1.Roles.CreateRole;

/// <summary>Maps <c>POST /roles</c>.</summary>
public static class CreateRoleEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapCreateRoleEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/roles",
                (CreateRoleCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(command, cancellationToken))
            .WithName("CreateRole")
            .WithSummary("Create a role")
            .Produces<RoleDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequirePermission(IdentityPermissions.Roles.Create);
}
