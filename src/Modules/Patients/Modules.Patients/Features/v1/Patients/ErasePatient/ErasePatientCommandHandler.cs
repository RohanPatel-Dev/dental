using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Framework.Storage;
using Dental.Modules.Patients.Contracts.Events;
using Dental.Modules.Patients.Contracts.v1.Patients.ErasePatient;
using Dental.Modules.Patients.Data;
using Dental.Modules.Patients.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Patients.Features.v1.Patients.ErasePatient;

/// <summary>
/// Erases a patient's identifiers here, deletes their files, and announces the erasure so every
/// other module drops its own copy.
/// </summary>
/// <remarks>
/// This module can only erase what it owns. Appointments, clinical notes and invoices live in other
/// schemas that this handler must not touch - the integration event is what makes the erasure
/// complete, and each module's handler is responsible for its own tables.
/// </remarks>
/// <param name="context">The patients context.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="storage">Object storage, for the attached documents.</param>
/// <param name="timeProvider">Clock.</param>
/// <param name="logger">Logger.</param>
public sealed class ErasePatientCommandHandler(
    PatientsDbContext context,
    IOutboxStore outbox,
    IStorageService storage,
    TimeProvider timeProvider,
    ILogger<ErasePatientCommandHandler> logger)
    : ICommandHandler<ErasePatientCommand, ErasePatientResponse>
{
    /// <inheritdoc />
    public async ValueTask<ErasePatientResponse> Handle(
        ErasePatientCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Patient patient = await context.Patients
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Patient", command.PatientId);

        if (patient.IsErased)
        {
            throw new ConflictException("This patient record has already been erased.");
        }

        foreach (PatientDocument document in patient.Documents)
        {
            await storage.RemoveAsync(document.StorageKey, cancellationToken).ConfigureAwait(false);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        patient.Erase(now);

        await outbox.AddAsync(
                new PatientErasureRequestedIntegrationEvent(patient.Id, command.Reason)
                {
                    TenantId = patient.TenantId,
                    Source = nameof(Patients),
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Deliberately logs the identifier and nothing else: an erasure log line that quotes the
        // patient's name defeats the erasure.
        logger.LogWarning(
            "Erased patient {PatientId} and announced the erasure to other modules.",
            patient.Id);

        return new ErasePatientResponse(patient.Id, now);
    }
}
