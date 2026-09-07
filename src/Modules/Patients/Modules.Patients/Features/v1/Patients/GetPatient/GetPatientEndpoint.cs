using Dental.Framework.Web.Auth;
using Dental.Modules.Patients.Contracts.Authorization;
using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.v1.Patients.GetPatient;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Patients.Features.v1.Patients.GetPatient;

/// <summary>Maps <c>GET /patients/{id:guid}</c>.</summary>
public static class GetPatientEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapGetPatientEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/patients/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    mediator.Send(new GetPatientQuery(id), cancellationToken))
            .WithName("GetPatient")
            .WithSummary("Read one patient record")
            .Produces<PatientDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(PatientsPermissions.View);
}
