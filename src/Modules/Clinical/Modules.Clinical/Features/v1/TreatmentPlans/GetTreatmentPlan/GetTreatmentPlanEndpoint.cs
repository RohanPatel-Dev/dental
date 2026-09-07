using Dental.Framework.Web.Auth;
using Dental.Modules.Clinical.Contracts.Authorization;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.GetTreatmentPlan;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.GetTreatmentPlan;

/// <summary>Maps <c>GET /treatment-plans/{id:guid}</c>.</summary>
public static class GetTreatmentPlanEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapGetTreatmentPlanEndpoint(
        this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/treatment-plans/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(new GetTreatmentPlanQuery(id), cancellationToken))
            .WithName("GetTreatmentPlan")
            .WithSummary("Read one treatment plan")
            .Produces<TreatmentPlanDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(ClinicalPermissions.Treatment.View);
}
