using Dental.Framework.Shared.Pagination;
using Dental.Modules.Clinical.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.SearchTreatmentPlans;

/// <summary>Pages through treatment plans.</summary>
/// <param name="PatientId">Filters to one patient.</param>
/// <param name="ProviderId">Filters to one provider.</param>
/// <param name="Status">Filters by lifecycle status.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchTreatmentPlansQuery(
    Guid? PatientId,
    Guid? ProviderId,
    TreatmentPlanStatus? Status,
    int PageNumber,
    int PageSize,
    string? Sort) : IQuery<PagedResponse<TreatmentPlanDto>>, IPagedQuery;
