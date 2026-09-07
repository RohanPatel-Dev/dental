using Dental.Framework.Core.Exceptions;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Contracts.Services;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.GetAppointment;
using Mediator;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.GetAppointment;

/// <summary>Reads one appointment.</summary>
/// <param name="appointments">Appointment lookups.</param>
public sealed class GetAppointmentQueryHandler(IAppointmentService appointments)
    : IQueryHandler<GetAppointmentQuery, AppointmentDto>
{
    /// <inheritdoc />
    public async ValueTask<AppointmentDto> Handle(
        GetAppointmentQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await appointments.GetAsync(query.AppointmentId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Appointment", query.AppointmentId);
    }
}
