using Dental.Framework.Web.Auth;
using Dental.Modules.Tenancy.Contracts.Authorization;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Dental.Modules.Tenancy.Contracts.v1.Tenants.GetTenant;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.GetTenant;

/// <summary>Maps <c>GET /tenants/{identifier}</c>.</summary>
public static class GetTenantEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapGetTenantEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/tenants/{identifier}",
                (string identifier, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(new GetTenantQuery(identifier), cancellationToken))
            .WithName("GetTenant")
            .WithSummary("Read one tenant")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(TenancyPermissions.View);
}
