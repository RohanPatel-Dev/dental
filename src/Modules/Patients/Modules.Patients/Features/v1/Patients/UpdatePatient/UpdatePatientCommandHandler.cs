using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.Events;
using Dental.Modules.Patients.Contracts.v1.Patients.UpdatePatient;
using Dental.Modules.Patients.Data;
using Dental.Modules.Patients.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Patients.Features.v1.Patients.UpdatePatient;

/// <summary>Amends a patient record.</summary>
/// <param name="context">The patients context.</param>
/// <param name="outbox">Outbox writer.</param>
public sealed class UpdatePatientCommandHandler(PatientsDbContext context, IOutboxStore<PatientsDbContext> outbox)
    : ICommandHandler<UpdatePatientCommand, PatientDto>
{
    /// <inheritdoc />
    public async ValueTask<PatientDto> Handle(
        UpdatePatientCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Tracked deliberately: read-then-mutate. AsNoTracking here would drop the edit silently.
        Patient patient = await context.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Patient", command.PatientId);

        if (patient.IsErased)
        {
            throw new ConflictException("An erased patient record cannot be amended.");
        }

        patient.FirstName = command.FirstName.Trim();
        patient.LastName = command.LastName.Trim();
        patient.Email = command.Email?.Trim();
        patient.PhoneNumber = command.PhoneNumber?.Trim();
        patient.Status = command.Status;
        patient.PreferredProviderId = command.PreferredProviderId;
        patient.Allergies = [.. command.Allergies];

        // Announced so that projections in other processes - the Notifications host keeps one -
        // do not go on addressing the patient by an old name or mailing an old address.
        await outbox.AddAsync(
                new PatientContactChangedIntegrationEvent(
                    patient.Id,
                    patient.FullName,
                    patient.Email,
                    patient.PhoneNumber)
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
