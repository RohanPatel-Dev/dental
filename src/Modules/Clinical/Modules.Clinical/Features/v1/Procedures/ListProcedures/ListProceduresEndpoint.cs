using Dental.Framework.Web.Auth;
using Dental.Modules.Clinical.Contracts.Authorization;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.Procedures.ListProcedures;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Clinical.Features.v1.Procedures.ListProcedures;

/// <summary>Maps <c>GET /procedures</c>.</summary>
public static class ListProceduresEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapListProceduresEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/procedures",
                (
                    ProcedureCategory? category,
                    bool? onlyActive,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new ListProceduresQuery(category, onlyActive ?? true),
                        cancellationToken))
            .WithName("ListProcedures")
            .WithSummary("List the practice's procedure catalog")
            .Produces<IReadOnlyList<ProcedureDto>>(StatusCodes.Status200OK)
            .RequirePermission(ClinicalPermissions.Procedures.View);
}
