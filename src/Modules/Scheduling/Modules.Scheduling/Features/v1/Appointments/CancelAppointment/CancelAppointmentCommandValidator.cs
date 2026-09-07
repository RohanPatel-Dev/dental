using Dental.Modules.Scheduling.Contracts.v1.Appointments.CancelAppointment;
using FluentValidation;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.CancelAppointment;

/// <summary>Validates <see cref="CancelAppointmentCommand"/>.</summary>
public sealed class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    /// <summary>Builds the rules.</summary>
    public CancelAppointmentCommandValidator()
    {
        RuleFor(c => c.AppointmentId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(512);
    }
}
