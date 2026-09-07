using Dental.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Scheduling.Contracts.v1.Appointments.GetAppointment;

/// <summary>Reads one appointment.</summary>
/// <param name="AppointmentId">Appointment identifier.</param>
public sealed record GetAppointmentQuery(Guid AppointmentId) : IQuery<AppointmentDto>;
