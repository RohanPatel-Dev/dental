using Dental.Framework.Web.Auth;
using Dental.Modules.Tenancy.Contracts.Authorization;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Dental.Modules.Tenancy.Contracts.v1.Tenants.ChangeTenantPlan;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.ChangeTenantPlan;

/// <summary>Maps <c>PUT /tenants/{identifier}/plan</c>.</summary>
public static class ChangeTenantPlanEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapChangeTenantPlanEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut(
                "/tenants/{identifier}/plan",
                (
                    string identifier,
                    ChangeTenantPlanRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new ChangeTenantPlanCommand(identifier, request.Plan),
                        cancellationToken))
            .WithName("ChangeTenantPlan")
            .WithSummary("Move a tenant onto a different plan")
            .Produces<TenantDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(TenancyPermissions.Manage);
}

/// <summary>Body shape for <see cref="ChangeTenantPlanEndpoint"/>.</summary>
/// <param name="Plan">The new plan name.</param>
public sealed record ChangeTenantPlanRequest(string Plan);
