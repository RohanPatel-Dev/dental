using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Contracts.Services;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.Services;
using Dental.Modules.Scheduling.Data;
using Dental.Modules.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Scheduling.Services;

/// <summary>The Scheduling module's implementation of its own public contract.</summary>
/// <param name="context">The scheduling context.</param>
/// <param name="patientService">Resolves patient names through the Patients module's contract.</param>
public sealed class AppointmentService(SchedulingDbContext context, IPatientService patientService)
    : IAppointmentService
{
    /// <inheritdoc />
    public async Task<AppointmentDto?> GetAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        Appointment? appointment = await context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            .ConfigureAwait(false);

        if (appointment is null)
        {
            return null;
        }

        IReadOnlyList<AppointmentDto> hydrated =
            await HydrateAsync([appointment], cancellationToken).ConfigureAwait(false);

        return hydrated[0];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AppointmentDto>> GetUpcomingAsync(
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken = default)
    {
        List<Appointment> appointments = await context.Appointments
            .AsNoTracking()
            .Where(a => a.StartsAt >= windowStart && a.StartsAt < windowEnd)
            .Where(a => a.Status == AppointmentStatus.Scheduled
                        || a.Status == AppointmentStatus.Confirmed)
            .OrderBy(a => a.StartsAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await HydrateAsync(appointments, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Resolves patient and provider display names for a set of appointments.
    /// </summary>
    /// <param name="appointments">The appointments to project.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The projected appointments.</returns>
    /// <remarks>
    /// Patient names come from <see cref="IPatientService"/> in ONE batched call rather than a join:
    /// the two modules own separate schemas, and reaching across them with SQL is exactly the
    /// coupling the architecture forbids.
    /// </remarks>
    internal async Task<IReadOnlyList<AppointmentDto>> HydrateAsync(
        IReadOnlyCollection<Appointment> appointments,
        CancellationToken cancellationToken)
    {
        if (appointments.Count == 0)
        {
            return [];
        }

        Guid[] patientIds = [.. appointments.Select(a => a.PatientId).Distinct()];
        Guid[] providerIds = [.. appointments.Select(a => a.ProviderId).Distinct()];

        IReadOnlyDictionary<Guid, PatientSummaryDto> patients = await patientService
            .GetSummariesAsync(patientIds, cancellationToken)
            .ConfigureAwait(false);

        Dictionary<Guid, string> providers = await context.Providers
            .AsNoTracking()
            .Where(p => providerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.DisplayName, cancellationToken)
            .ConfigureAwait(false);

        return
        [
            .. appointments.Select(a => new AppointmentDto(
                a.Id,
                a.PatientId,
                patients.TryGetValue(a.PatientId, out PatientSummaryDto? patient) ? patient.FullName : null,
                a.ProviderId,
                providers.TryGetValue(a.ProviderId, out string? providerName) ? providerName : null,
                a.OperatoryId,
                a.StartsAt,
                a.EndsAt,
                a.Kind,
                a.Status,
                a.Notes,
                a.CancellationReason)),
        ];
    }
}
