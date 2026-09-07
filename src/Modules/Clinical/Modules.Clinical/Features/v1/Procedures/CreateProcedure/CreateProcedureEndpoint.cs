using Dental.Framework.Web.Auth;
using Dental.Modules.Clinical.Contracts.Authorization;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.Procedures.CreateProcedure;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Clinical.Features.v1.Procedures.CreateProcedure;

/// <summary>Maps <c>POST /procedures</c>.</summary>
public static class CreateProcedureEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapCreateProcedureEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/procedures",
                (
                    CreateProcedureCommand command,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(command, cancellationToken))
            .WithName("CreateProcedure")
            .WithSummary("Add a procedure to the catalog")
            .Produces<ProcedureDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequirePermission(ClinicalPermissions.Procedures.Create);
}
