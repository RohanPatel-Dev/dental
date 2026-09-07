using Dental.Framework.Shared.Pagination;
using Dental.Framework.Web.Auth;
using Dental.Modules.Clinical.Contracts.Authorization;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.SearchTreatmentPlans;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.SearchTreatmentPlans;

/// <summary>Maps <c>GET /treatment-plans</c>.</summary>
public static class SearchTreatmentPlansEndpoint
{
    /// <summary>Registers the endpoint.</summary>
    /// <param name="endpoints">The route group.</param>
    /// <returns>The route handler builder, for further configuration.</returns>
    internal static RouteHandlerBuilder MapSearchTreatmentPlansEndpoint(
        this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet(
                "/treatment-plans",
                (
                    [AsParameters] SearchTreatmentPlansRequest request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                    mediator.Send(
                        new SearchTreatmentPlansQuery(
                            request.PatientId,
                            request.ProviderId,
                            request.Status,
                            request.PageNumber ?? PaginationDefaults.MinPageNumber,
                            request.PageSize ?? PaginationDefaults.DefaultPageSize,
                            request.Sort),
                        cancellationToken))
            .WithName("SearchTreatmentPlans")
            .WithSummary("Page through treatment plans")
            .Produces<PagedResponse<TreatmentPlanDto>>(StatusCodes.Status200OK)
            .RequirePermission(ClinicalPermissions.Treatment.Search);
}

/// <summary>Query string shape for <see cref="SearchTreatmentPlansEndpoint"/>.</summary>
/// <param name="PatientId">Filters to one patient.</param>
/// <param name="ProviderId">Filters to one provider.</param>
/// <param name="Status">Filters by lifecycle status.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchTreatmentPlansRequest(
    Guid? PatientId,
    Guid? ProviderId,
    TreatmentPlanStatus? Status,
    int? PageNumber,
    int? PageSize,
    string? Sort);
