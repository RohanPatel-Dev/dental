using Dental.Framework.Web.Auth;
using Dental.Framework.Web.Middleware;
using Dental.Modules.Clinical.Contracts.Authorization;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.AcceptTreatmentPlan;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.AcceptTreatmentPlan;

/// <summary>Maps <c>POST /treatment-plans/{id:guid}/acceptance</c>.</summary>
public static class AcceptTreatmentPlanEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapAcceptTreatmentPlanEndpoint(
        this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/treatment-plans/{id:guid}/acceptance",
                (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(new AcceptTreatmentPlanCommand(id), cancellationToken))
            .WithName("AcceptTreatmentPlan")
            .WithSummary("Record the patient's acceptance of a treatment plan")
            .Produces<TreatmentPlanDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequirePermission(ClinicalPermissions.Treatment.Update)
            .WithIdempotency();
}
