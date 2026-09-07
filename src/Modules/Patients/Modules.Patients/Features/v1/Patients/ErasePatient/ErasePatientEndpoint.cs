using Dental.Framework.Web.Auth;
using Dental.Modules.Patients.Contracts.Authorization;
using Dental.Modules.Patients.Contracts.v1.Patients.ErasePatient;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Patients.Features.v1.Patients.ErasePatient;

/// <summary>Maps <c>DELETE /patients/{id:guid}</c>.</summary>
public static class ErasePatientEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapErasePatientEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapDelete(
                "/patients/{id:guid}",
                (
                    Guid id,
                    string reason,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(new ErasePatientCommand(id, reason), cancellationToken))
            .WithName("ErasePatient")
            .WithSummary("Erase a patient's personal data")
            .Produces<ErasePatientResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(PatientsPermissions.Delete);
}
