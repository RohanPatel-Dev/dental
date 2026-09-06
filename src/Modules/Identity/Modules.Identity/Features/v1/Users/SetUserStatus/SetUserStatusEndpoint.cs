using Dental.Framework.Web.Auth;
using Dental.Modules.Identity.Contracts.Authorization;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Users.SetUserStatus;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Identity.Features.v1.Users.SetUserStatus;

/// <summary>Maps <c>PUT /users/{id:guid}/status</c>.</summary>
public static class SetUserStatusEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapSetUserStatusEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut(
                "/users/{id:guid}/status",
                (
                    Guid id,
                    SetUserStatusRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(new SetUserStatusCommand(id, request.IsActive), cancellationToken))
            .WithName("SetUserStatus")
            .WithSummary("Activate or deactivate a staff account")
            .Produces<UserDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(IdentityPermissions.Users.Update);
}

/// <summary>Body shape for <see cref="SetUserStatusEndpoint"/>.</summary>
/// <param name="IsActive">Target state.</param>
public sealed record SetUserStatusRequest(bool IsActive);
