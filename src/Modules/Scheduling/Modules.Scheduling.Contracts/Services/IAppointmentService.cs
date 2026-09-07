using Dental.Modules.Scheduling.Contracts.Dtos;

namespace Dental.Modules.Scheduling.Contracts.Services;

/// <summary>The Scheduling module's public surface.</summary>
public interface IAppointmentService
{
    /// <summary>Reads one appointment.</summary>
    /// <param name="appointmentId">Appointment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The appointment, or null when it does not exist in this tenant.</returns>
    Task<AppointmentDto?> GetAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    /// <summary>Reads the appointments due to start inside a window, across every provider.</summary>
    /// <param name="windowStart">Earliest start time.</param>
    /// <param name="windowEnd">Latest start time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The appointments due in that window.</returns>
    Task<IReadOnlyList<AppointmentDto>> GetUpcomingAsync(
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken = default);
}
