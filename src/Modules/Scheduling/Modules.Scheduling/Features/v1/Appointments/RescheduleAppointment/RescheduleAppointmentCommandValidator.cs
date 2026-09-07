using Dental.Modules.Scheduling.Contracts.v1.Appointments.RescheduleAppointment;
using Dental.Modules.Scheduling.Features.v1.Appointments.BookAppointment;
using FluentValidation;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.RescheduleAppointment;

/// <summary>Validates <see cref="RescheduleAppointmentCommand"/>.</summary>
public sealed class RescheduleAppointmentCommandValidator
    : AbstractValidator<RescheduleAppointmentCommand>
{
    /// <summary>Builds the rules.</summary>
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(c => c.AppointmentId).NotEmpty();

        RuleFor(c => c.DurationMinutes)
            .InclusiveBetween(
                BookAppointmentCommandValidator.MinimumDurationMinutes,
                BookAppointmentCommandValidator.MaximumDurationMinutes);

        RuleFor(c => c.StartsAt)
            .NotEqual(default(DateTimeOffset))
            .Must(start => start < DateTimeOffset.UtcNow.AddYears(3))
            .WithMessage("StartsAt is implausibly far in the future.");
    }
}
