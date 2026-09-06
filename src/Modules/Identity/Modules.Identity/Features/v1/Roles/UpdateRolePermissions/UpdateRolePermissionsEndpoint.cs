using Dental.Framework.Web.Auth;
using Dental.Modules.Identity.Contracts.Authorization;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Roles.UpdateRolePermissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Identity.Features.v1.Roles.UpdateRolePermissions;

/// <summary>Maps <c>PUT /roles/{id:guid}/permissions</c>.</summary>
public static class UpdateRolePermissionsEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapUpdateRolePermissionsEndpoint(
        this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut(
                "/roles/{id:guid}/permissions",
                (
                    Guid id,
                    UpdateRolePermissionsRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new UpdateRolePermissionsCommand(id, request.Permissions),
                        cancellationToken))
            .WithName("UpdateRolePermissions")
            .WithSummary("Replace the permissions granted by a role")
            .Produces<RoleDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(IdentityPermissions.Roles.Update);
}

/// <summary>Body shape for <see cref="UpdateRolePermissionsEndpoint"/>.</summary>
/// <param name="Permissions">The complete set of permissions the role should grant.</param>
public sealed record UpdateRolePermissionsRequest(IReadOnlyList<string> Permissions);
