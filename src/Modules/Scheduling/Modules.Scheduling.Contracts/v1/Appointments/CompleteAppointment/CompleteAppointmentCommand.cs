using Dental.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Scheduling.Contracts.v1.Appointments.CompleteAppointment;

/// <summary>Marks an appointment complete, which starts the clinical and billing follow-on.</summary>
/// <param name="AppointmentId">Appointment identifier.</param>
public sealed record CompleteAppointmentCommand(Guid AppointmentId) : ICommand<AppointmentDto>;
