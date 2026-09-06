using Dental.Framework.Web.Auth;
using Dental.Modules.Tenancy.Contracts.Authorization;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Dental.Modules.Tenancy.Contracts.v1.Tenants.SetTenantStatus;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.SetTenantStatus;

/// <summary>Maps <c>PUT /tenants/{identifier}/status</c>.</summary>
public static class SetTenantStatusEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapSetTenantStatusEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut(
                "/tenants/{identifier}/status",
                (
                    string identifier,
                    SetTenantStatusRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new SetTenantStatusCommand(identifier, request.IsActive, request.Reason),
                        cancellationToken))
            .WithName("SetTenantStatus")
            .WithSummary("Activate or deactivate a tenant")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(TenancyPermissions.Manage);
}

/// <summary>Body shape for <see cref="SetTenantStatusEndpoint"/>.</summary>
/// <param name="IsActive">Target state.</param>
/// <param name="Reason">Why the change is being made. Required when deactivating.</param>
public sealed record SetTenantStatusRequest(bool IsActive, string? Reason);
