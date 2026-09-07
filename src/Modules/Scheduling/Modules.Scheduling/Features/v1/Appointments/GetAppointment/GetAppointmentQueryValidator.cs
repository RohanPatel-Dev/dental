using Dental.Modules.Scheduling.Contracts.v1.Appointments.GetAppointment;
using FluentValidation;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.GetAppointment;

/// <summary>Validates <see cref="GetAppointmentQuery"/>.</summary>
public sealed class GetAppointmentQueryValidator : AbstractValidator<GetAppointmentQuery>
{
    /// <summary>Builds the rules.</summary>
    public GetAppointmentQueryValidator() => RuleFor(q => q.AppointmentId).NotEmpty();
}
