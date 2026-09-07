using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.Events;
using Dental.Modules.Patients.Contracts.v1.Patients.UpdateConsent;
using Dental.Modules.Patients.Data;
using Dental.Modules.Patients.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Patients.Features.v1.Patients.UpdateConsent;

/// <summary>
/// Records a consent change and announces it, so the Notifications module stops or resumes contact.
/// </summary>
/// <param name="context">The patients context.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="timeProvider">Clock.</param>
public sealed class UpdateConsentCommandHandler(
    PatientsDbContext context,
    IOutboxStore outbox,
    TimeProvider timeProvider) : ICommandHandler<UpdateConsentCommand, PatientDto>
{
    /// <inheritdoc />
    public async ValueTask<PatientDto> Handle(
        UpdateConsentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Patient patient = await context.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Patient", command.PatientId);

        patient.RecordConsent(
            command.HasMarketingConsent,
            command.HasReminderConsent,
            timeProvider.GetUtcNow());

        // Consent withdrawal has to reach every module that might contact the patient, and it has to
        // survive a crash between the write and the announcement - hence the outbox.
        await outbox.AddAsync(
                new PatientConsentChangedIntegrationEvent(
                    patient.Id,
                    command.HasMarketingConsent,
                    command.HasReminderConsent)
                {
                    TenantId = patient.TenantId,
                    Source = nameof(Patients),
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return PatientMapper.ToDto(patient);
    }
}
