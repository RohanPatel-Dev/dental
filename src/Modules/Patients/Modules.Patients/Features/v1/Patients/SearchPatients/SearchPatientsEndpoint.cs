using Dental.Framework.Shared.Pagination;
using Dental.Framework.Web.Auth;
using Dental.Modules.Patients.Contracts.Authorization;
using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.v1.Patients.SearchPatients;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Patients.Features.v1.Patients.SearchPatients;

/// <summary>Maps <c>GET /patients</c>.</summary>
public static class SearchPatientsEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapSearchPatientsEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/patients",
                (
                    [AsParameters] SearchPatientsRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new SearchPatientsQuery(
                            request.SearchTerm,
                            request.Status,
                            request.PreferredProviderId,
                            request.PageNumber ?? PaginationDefaults.MinPageNumber,
                            request.PageSize ?? PaginationDefaults.DefaultPageSize,
                            request.Sort),
                        cancellationToken))
            .WithName("SearchPatients")
            .WithSummary("Page through the practice's patients")
            .Produces<PagedResponse<PatientDto>>(StatusCodes.Status200OK)
            .RequirePermission(PatientsPermissions.Search);
}

/// <summary>Query string shape for <see cref="SearchPatientsEndpoint"/>.</summary>
/// <param name="SearchTerm">Matched against chart number, name, email and phone.</param>
/// <param name="Status">Filters by record status.</param>
/// <param name="PreferredProviderId">Filters by the patient's usual provider.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchPatientsRequest(
    string? SearchTerm,
    PatientStatus? Status,
    Guid? PreferredProviderId,
    int? PageNumber,
    int? PageSize,
    string? Sort);
