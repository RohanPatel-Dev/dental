using Dental.Framework.Web.Auth;
using Dental.Modules.Identity.Contracts.Authorization;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Roles.ListRoles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Identity.Features.v1.Roles.ListRoles;

/// <summary>Maps <c>GET /roles</c>.</summary>
public static class ListRolesEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapListRolesEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/roles",
                (IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(new ListRolesQuery(), cancellationToken))
            .WithName("ListRoles")
            .WithSummary("List the roles of the current practice")
            .Produces<IReadOnlyList<RoleDto>>(StatusCodes.Status200OK)
            .RequirePermission(IdentityPermissions.Roles.View);
}
