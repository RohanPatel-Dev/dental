using Dental.Framework.Web.Auth;
using Dental.Framework.Web.Middleware;
using Dental.Modules.Clinical.Contracts.Authorization;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.CreateTreatmentPlan;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.CreateTreatmentPlan;

/// <summary>Maps <c>POST /treatment-plans</c>.</summary>
public static class CreateTreatmentPlanEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapCreateTreatmentPlanEndpoint(
        this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/treatment-plans",
                (
                    CreateTreatmentPlanCommand command,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(command, cancellationToken))
            .WithName("CreateTreatmentPlan")
            .WithSummary("Author a treatment plan")
            .Produces<TreatmentPlanDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(ClinicalPermissions.Treatment.Create)
            .WithIdempotency();
}
