using Dental.Framework.Persistence.Pagination;
using Dental.Framework.Shared.Pagination;
using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.v1.Patients.SearchPatients;
using Dental.Modules.Patients.Data;
using Dental.Modules.Patients.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Patients.Features.v1.Patients.SearchPatients;

/// <summary>Pages through the practice's patients.</summary>
/// <param name="context">The patients context.</param>
public sealed class SearchPatientsQueryHandler(PatientsDbContext context)
    : IQueryHandler<SearchPatientsQuery, PagedResponse<PatientDto>>
{
    /// <inheritdoc />
    public async ValueTask<PagedResponse<PatientDto>> Handle(
        SearchPatientsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Patient> patients = context.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = $"%{query.SearchTerm.Trim()}%";
            patients = patients.Where(p =>
                EF.Functions.ILike(p.ChartNumber, term)
                || EF.Functions.ILike(p.FirstName, term)
                || EF.Functions.ILike(p.LastName, term)
                || (p.Email != null && EF.Functions.ILike(p.Email, term))
                || (p.PhoneNumber != null && EF.Functions.ILike(p.PhoneNumber, term)));
        }

        if (query.Status is { } status)
        {
            patients = patients.Where(p => p.Status == status);
        }

        if (query.PreferredProviderId is { } providerId)
        {
            patients = patients.Where(p => p.PreferredProviderId == providerId);
        }

        patients = query.Sort switch
        {
            "chartNumber asc" => patients.OrderBy(p => p.ChartNumber),
            "chartNumber desc" => patients.OrderByDescending(p => p.ChartNumber),
            "createdAt desc" => patients.OrderByDescending(p => p.CreatedAt),
            _ => patients.OrderBy(p => p.LastName).ThenBy(p => p.FirstName),
        };

        return await patients
            .Select(p => new PatientDto(
                p.Id,
                p.ChartNumber,
                p.FirstName,
                p.LastName,
                p.DateOfBirth,
                p.Sex,
                p.Email,
                p.PhoneNumber,
                p.Status,
                p.PreferredProviderId,
                p.Allergies,
                p.HasMarketingConsent,
                p.HasReminderConsent,
                p.CreatedAt))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
