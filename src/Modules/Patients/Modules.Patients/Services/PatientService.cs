using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.Services;
using Dental.Modules.Patients.Data;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Patients.Services;

/// <summary>The Patients module's implementation of its own public contract.</summary>
/// <param name="context">The patients context.</param>
public sealed class PatientService(PatientsDbContext context) : IPatientService
{
    /// <inheritdoc />
    public async Task<PatientSummaryDto?> GetSummaryAsync(
        Guid patientId,
        CancellationToken cancellationToken = default) =>
        await context.Patients
            .AsNoTracking()
            .Where(p => p.Id == patientId)
            .Select(p => new PatientSummaryDto(
                p.Id,
                p.ChartNumber,
                p.FirstName + " " + p.LastName,
                p.Email,
                p.PhoneNumber,
                p.HasReminderConsent))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, PatientSummaryDto>> GetSummariesAsync(
        IReadOnlyCollection<Guid> patientIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patientIds);

        if (patientIds.Count == 0)
        {
            return new Dictionary<Guid, PatientSummaryDto>();
        }

        List<PatientSummaryDto> summaries = await context.Patients
            .AsNoTracking()
            .Where(p => patientIds.Contains(p.Id))
            .Select(p => new PatientSummaryDto(
                p.Id,
                p.ChartNumber,
                p.FirstName + " " + p.LastName,
                p.Email,
                p.PhoneNumber,
                p.HasReminderConsent))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return summaries.ToDictionary(s => s.Id);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        context.Patients.AnyAsync(
            p => p.Id == patientId && p.Status == PatientStatus.Active,
            cancellationToken);

    /// <inheritdoc />
    public Task<long> CountAsync(string tenantId, CancellationToken cancellationToken = default) =>
        context.Patients
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId && p.DeletedAt == null)
            .LongCountAsync(cancellationToken);
}
