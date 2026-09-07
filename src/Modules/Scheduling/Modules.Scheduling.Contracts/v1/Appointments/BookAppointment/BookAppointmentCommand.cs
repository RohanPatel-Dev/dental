using Dental.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Scheduling.Contracts.v1.Appointments.BookAppointment;

/// <summary>Books an appointment.</summary>
/// <param name="PatientId">Patient the appointment is for.</param>
/// <param name="ProviderId">Provider delivering it.</param>
/// <param name="OperatoryId">Chair it occupies.</param>
/// <param name="StartsAt">Start time, in UTC.</param>
/// <param name="DurationMinutes">How long to book.</param>
/// <param name="Kind">What the appointment is for.</param>
/// <param name="Notes">Front desk notes. Never clinical detail.</param>
public sealed record BookAppointmentCommand(
    Guid PatientId,
    Guid ProviderId,
    Guid OperatoryId,
    DateTimeOffset StartsAt,
    int DurationMinutes,
    AppointmentKind Kind,
    string? Notes) : ICommand<AppointmentDto>;
