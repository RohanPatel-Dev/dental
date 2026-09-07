using Dental.Framework.Shared.Pagination;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Scheduling.Contracts.v1.Appointments.SearchAppointments;

/// <summary>Pages through the appointment book.</summary>
/// <param name="PatientId">Filters to one patient.</param>
/// <param name="ProviderId">Filters to one provider.</param>
/// <param name="OperatoryId">Filters to one chair.</param>
/// <param name="Status">Filters by lifecycle status.</param>
/// <param name="From">Earliest start time.</param>
/// <param name="To">Latest start time.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchAppointmentsQuery(
    Guid? PatientId,
    Guid? ProviderId,
    Guid? OperatoryId,
    AppointmentStatus? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int PageNumber,
    int PageSize,
    string? Sort) : IQuery<PagedResponse<AppointmentDto>>, IPagedQuery;
