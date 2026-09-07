using Dental.Framework.Persistence.Pagination;
using Dental.Framework.Shared.Pagination;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.SearchAppointments;
using Dental.Modules.Scheduling.Data;
using Dental.Modules.Scheduling.Domain;
using Dental.Modules.Scheduling.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.SearchAppointments;

/// <summary>Pages through the appointment book.</summary>
/// <param name="context">The scheduling context.</param>
/// <param name="appointments">Resolves display names for the page.</param>
public sealed class SearchAppointmentsQueryHandler(
    SchedulingDbContext context,
    AppointmentService appointments)
    : IQueryHandler<SearchAppointmentsQuery, PagedResponse<AppointmentDto>>
{
    /// <inheritdoc />
    public async ValueTask<PagedResponse<AppointmentDto>> Handle(
        SearchAppointmentsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Appointment> filtered = context.Appointments.AsNoTracking();

        if (query.PatientId is { } patientId)
        {
            filtered = filtered.Where(a => a.PatientId == patientId);
        }

        if (query.ProviderId is { } providerId)
        {
            filtered = filtered.Where(a => a.ProviderId == providerId);
        }

        if (query.OperatoryId is { } operatoryId)
        {
            filtered = filtered.Where(a => a.OperatoryId == operatoryId);
        }

        if (query.Status is { } status)
        {
            filtered = filtered.Where(a => a.Status == status);
        }

        if (query.From is { } from)
        {
            filtered = filtered.Where(a => a.StartsAt >= from);
        }

        if (query.To is { } to)
        {
            filtered = filtered.Where(a => a.StartsAt < to);
        }

        filtered = query.Sort == "startsAt desc"
            ? filtered.OrderByDescending(a => a.StartsAt)
            : filtered.OrderBy(a => a.StartsAt);

        PagedResponse<Appointment> page = await filtered
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<AppointmentDto> items = await appointments
            .HydrateAsync(page.Items, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<AppointmentDto>(
            items,
            page.PageNumber,
            page.PageSize,
            page.TotalCount);
    }
}
