using Dental.Framework.Web.Auth;
using Dental.Framework.Web.Middleware;
using Dental.Modules.Patients.Contracts.Authorization;
using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.v1.Patients.RegisterPatient;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Patients.Features.v1.Patients.RegisterPatient;

/// <summary>Maps <c>POST /patients</c>.</summary>
public static class RegisterPatientEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapRegisterPatientEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost(
                "/patients",
                (
                    RegisterPatientCommand command,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(command, cancellationToken))
            .WithName("RegisterPatient")
            .WithSummary("Register a new patient")
            .Produces<PatientDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .RequirePermission(PatientsPermissions.Create)
            .WithIdempotency();
}
