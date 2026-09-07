using Dental.Framework.Eventing.Abstractions;
using Dental.Modules.Patients.Contracts.Events;
using Dental.Modules.Scheduling.Data;
using Dental.Modules.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Scheduling.Events;

/// <summary>
/// Strips the free-text fields from a patient's appointments when their record is erased.
/// </summary>
/// <remarks>
/// The appointments themselves stay: a practice must keep an attendance history, and the rows carry
/// no identifiers once the notes are cleared. This module erases only what it owns - the Patients
/// module cannot reach into this schema, and must not try.
/// </remarks>
/// <param name="context">The scheduling context.</param>
/// <param name="logger">Logger.</param>
public sealed class PatientErasureHandler(
    SchedulingDbContext context,
    ILogger<PatientErasureHandler> logger)
    : IIntegrationEventHandler<PatientErasureRequestedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        PatientErasureRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        List<Appointment> appointments = await context.Appointments
            .Where(a => a.PatientId == integrationEvent.PatientId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (Appointment appointment in appointments)
        {
            appointment.Erase();
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Erased free-text fields on {Count} appointment(s) for patient {PatientId}.",
            appointments.Count,
            integrationEvent.PatientId);
    }
}
