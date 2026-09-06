using Dental.Framework.Web.Auth;
using Dental.Modules.Identity.Contracts.Authorization;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Users.AssignRoles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Identity.Features.v1.Users.AssignRoles;

/// <summary>Maps <c>PUT /users/{id:guid}/roles</c>.</summary>
public static class AssignRolesEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapAssignRolesEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut(
                "/users/{id:guid}/roles",
                (
                    Guid id,
                    AssignRolesRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(new AssignRolesCommand(id, request.Roles), cancellationToken))
            .WithName("AssignRoles")
            .WithSummary("Replace a staff account's roles")
            .Produces<UserDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(IdentityPermissions.Users.Update);
}

/// <summary>Body shape for <see cref="AssignRolesEndpoint"/>.</summary>
/// <param name="Roles">The complete set of roles the user should hold.</param>
public sealed record AssignRolesRequest(IReadOnlyList<string> Roles);
