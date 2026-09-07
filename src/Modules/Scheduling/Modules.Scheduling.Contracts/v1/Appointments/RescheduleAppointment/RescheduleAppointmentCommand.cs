using Dental.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Scheduling.Contracts.v1.Appointments.RescheduleAppointment;

/// <summary>Moves an appointment to a new slot.</summary>
/// <param name="AppointmentId">Appointment identifier.</param>
/// <param name="StartsAt">New start time, in UTC.</param>
/// <param name="DurationMinutes">New duration.</param>
/// <param name="OperatoryId">New chair, when it changes.</param>
public sealed record RescheduleAppointmentCommand(
    Guid AppointmentId,
    DateTimeOffset StartsAt,
    int DurationMinutes,
    Guid? OperatoryId) : ICommand<AppointmentDto>;
