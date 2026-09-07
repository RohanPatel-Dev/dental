using Dental.Framework.Web.Auth;
using Dental.Modules.Patients.Contracts.Authorization;
using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.v1.Patients.UpdateConsent;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Patients.Features.v1.Patients.UpdateConsent;

/// <summary>Maps <c>PUT /patients/{id:guid}/consent</c>.</summary>
public static class UpdateConsentEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapUpdateConsentEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut(
                "/patients/{id:guid}/consent",
                (
                    Guid id,
                    UpdateConsentRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new UpdateConsentCommand(
                            id,
                            request.HasMarketingConsent,
                            request.HasReminderConsent),
                        cancellationToken))
            .WithName("UpdatePatientConsent")
            .WithSummary("Record a change to a patient's contact consent")
            .Produces<PatientDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequirePermission(PatientsPermissions.Update);
}

/// <summary>Body shape for <see cref="UpdateConsentEndpoint"/>.</summary>
/// <param name="HasMarketingConsent">Whether marketing contact is permitted.</param>
/// <param name="HasReminderConsent">Whether appointment reminders are permitted.</param>
public sealed record UpdateConsentRequest(bool HasMarketingConsent, bool HasReminderConsent);
