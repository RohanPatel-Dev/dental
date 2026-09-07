using Dental.Framework.Web.Auth;
using Dental.Modules.Patients.Contracts.Authorization;
using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.v1.Patients.UpdatePatient;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Patients.Features.v1.Patients.UpdatePatient;

/// <summary>Maps <c>PUT /patients/{id:guid}</c>.</summary>
public static class UpdatePatientEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapUpdatePatientEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut(
                "/patients/{id:guid}",
                (
                    Guid id,
                    UpdatePatientRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new UpdatePatientCommand(
                            id,
                            request.FirstName,
                            request.LastName,
                            request.Email,
                            request.PhoneNumber,
                            request.Status,
                            request.PreferredProviderId,
                            request.Allergies),
                        cancellationToken))
            .WithName("UpdatePatient")
            .WithSummary("Amend a patient record")
            .Produces<PatientDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(PatientsPermissions.Update);
}

/// <summary>Body shape for <see cref="UpdatePatientEndpoint"/>.</summary>
/// <param name="FirstName">Given name.</param>
/// <param name="LastName">Family name.</param>
/// <param name="Email">Contact address.</param>
/// <param name="PhoneNumber">Contact number.</param>
/// <param name="Status">Record status.</param>
/// <param name="PreferredProviderId">Provider the patient normally sees.</param>
/// <param name="Allergies">Recorded allergies.</param>
public sealed record UpdatePatientRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    PatientStatus Status,
    Guid? PreferredProviderId,
    IReadOnlyList<string> Allergies);
