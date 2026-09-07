using Dental.Framework.Core.Exceptions;
using Dental.Modules.Scheduling.Data;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Scheduling.Services;

/// <summary>Rejects a booking that would double-book a provider or a chair.</summary>
/// <param name="context">The scheduling context.</param>
public sealed class SlotAvailabilityChecker(SchedulingDbContext context)
{
    /// <summary>
    /// Throws when the requested slot overlaps a live appointment for the same provider or chair.
    /// </summary>
    /// <param name="providerId">Provider to check.</param>
    /// <param name="operatoryId">Chair to check.</param>
    /// <param name="startsAt">Proposed start time.</param>
    /// <param name="endsAt">Proposed end time.</param>
    /// <param name="excludingAppointmentId">Appointment being moved, excluded from the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the slot is confirmed free.</returns>
    /// <exception cref="ConflictException">The slot overlaps an existing appointment.</exception>
    /// <remarks>
    /// This is a check-then-act, so two simultaneous bookings can both pass it. That is an accepted
    /// trade for a dental practice: the front desk sees the clash immediately on the shared day view,
    /// and serializing every booking behind a lock would cost far more than it saves.
    /// </remarks>
    public async Task EnsureFreeAsync(
        Guid providerId,
        Guid operatoryId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Guid? excludingAppointmentId,
        CancellationToken cancellationToken = default)
    {
        bool clash = await context.Appointments
            .AsNoTracking()
            .Where(a => a.Id != excludingAppointmentId)
            .Where(a => a.Status != Contracts.Dtos.AppointmentStatus.Cancelled
                        && a.Status != Contracts.Dtos.AppointmentStatus.NoShow)
            .Where(a => a.ProviderId == providerId || a.OperatoryId == operatoryId)

            // Half-open overlap: [start, end). Two appointments that merely touch do not clash.
            .AnyAsync(a => a.StartsAt < endsAt && startsAt < a.EndsAt, cancellationToken)
            .ConfigureAwait(false);

        if (clash)
        {
            throw new ConflictException(
                "That slot overlaps an existing appointment for this provider or chair.");
        }
    }
}
