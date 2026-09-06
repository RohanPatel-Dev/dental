using Dental.Framework.Web.Auth;
using Dental.Framework.Web.Middleware;
using Dental.Modules.Tenancy.Contracts.Authorization;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Dental.Modules.Tenancy.Contracts.v1.Tenants.CreateTenant;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.CreateTenant;

/// <summary>Maps <c>POST /tenants</c>.</summary>
public static class CreateTenantEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapCreateTenantEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/tenants",
                (CreateTenantCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(command, cancellationToken))
            .WithName("CreateTenant")
            .WithSummary("Provision a new practice")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequirePermission(TenancyPermissions.Create)
            .WithIdempotency();
}
