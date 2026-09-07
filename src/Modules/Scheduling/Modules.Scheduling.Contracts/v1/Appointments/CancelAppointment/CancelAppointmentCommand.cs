using Dental.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Scheduling.Contracts.v1.Appointments.CancelAppointment;

/// <summary>Cancels an appointment.</summary>
/// <param name="AppointmentId">Appointment identifier.</param>
/// <param name="Reason">Why it is being cancelled.</param>
/// <param name="IsNoShow">True when the patient simply did not attend.</param>
public sealed record CancelAppointmentCommand(Guid AppointmentId, string Reason, bool IsNoShow)
    : ICommand<AppointmentDto>;
