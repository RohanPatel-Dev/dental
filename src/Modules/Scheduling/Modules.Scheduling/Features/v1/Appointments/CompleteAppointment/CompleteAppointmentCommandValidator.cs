using Dental.Modules.Scheduling.Contracts.v1.Appointments.CompleteAppointment;
using FluentValidation;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.CompleteAppointment;

/// <summary>Validates <see cref="CompleteAppointmentCommand"/>.</summary>
public sealed class CompleteAppointmentCommandValidator : AbstractValidator<CompleteAppointmentCommand>
{
    /// <summary>Builds the rules.</summary>
    public CompleteAppointmentCommandValidator() => RuleFor(c => c.AppointmentId).NotEmpty();
}
