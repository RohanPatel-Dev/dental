using Dental.Framework.Core.Exceptions;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.ChartEntries.RecordChartEntry;
using Dental.Modules.Clinical.Data;
using Dental.Modules.Clinical.Domain;
using Dental.Modules.Patients.Contracts.Services;
using Finbuckle.MultiTenant.Abstractions;
using Mediator;

namespace Dental.Modules.Clinical.Features.v1.ChartEntries.RecordChartEntry;

/// <summary>Records a finding against one tooth.</summary>
/// <param name="context">The clinical context.</param>
/// <param name="patientService">Confirms the patient exists in this tenant.</param>
/// <param name="tenantContextAccessor">Supplies the resolved tenant.</param>
/// <param name="timeProvider">Clock.</param>
public sealed class RecordChartEntryCommandHandler(
    ClinicalDbContext context,
    IPatientService patientService,
    IMultiTenantContextAccessor tenantContextAccessor,
    TimeProvider timeProvider) : ICommandHandler<RecordChartEntryCommand, ChartEntryDto>
{
    /// <inheritdoc />
    public async ValueTask<ChartEntryDto> Handle(
        RecordChartEntryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string tenantId = tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new ForbiddenException("No tenant is resolved for this request.");

        if (!await patientService.ExistsAsync(command.PatientId, cancellationToken).ConfigureAwait(false))
        {
            throw NotFoundException.For("Patient", command.PatientId);
        }

        // The chart is append only: a correction is a new entry, so the history of what was believed
        // when stays intact.
        ChartEntry entry = new()
        {
            PatientId = command.PatientId,
            ProviderId = command.ProviderId,
            ToothNumber = command.ToothNumber,
            Surfaces = command.Surfaces,
            Condition = command.Condition,
            Notes = command.Notes,
            RecordedAt = timeProvider.GetUtcNow(),
            TenantId = tenantId,
        };

        context.ChartEntries.Add(entry);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ChartEntryDto(
            entry.Id,
            entry.PatientId,
            entry.ToothNumber,
            entry.Surfaces,
            entry.Condition,
            entry.ProviderId,
            entry.RecordedAt,
            entry.Notes);
    }
}
