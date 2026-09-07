using Dental.Framework.Shared.Pagination;
using Dental.Modules.Patients.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Patients.Contracts.v1.Patients.SearchPatients;

/// <summary>Pages through the practice's patients.</summary>
/// <param name="SearchTerm">Matched against chart number, name, email and phone.</param>
/// <param name="Status">Filters by record status.</param>
/// <param name="PreferredProviderId">Filters by the patient's usual provider.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchPatientsQuery(
    string? SearchTerm,
    PatientStatus? Status,
    Guid? PreferredProviderId,
    int PageNumber,
    int PageSize,
    string? Sort) : IQuery<PagedResponse<PatientDto>>, IPagedQuery;
