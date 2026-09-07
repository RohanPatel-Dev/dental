using Dental.Modules.Scheduling.Contracts.v1.Appointments.BookAppointment;
using FluentValidation;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.BookAppointment;

/// <summary>Validates <see cref="BookAppointmentCommand"/>.</summary>
public sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    /// <summary>Shortest bookable slot, in minutes.</summary>
    public const int MinimumDurationMinutes = 5;

    /// <summary>Longest bookable slot, in minutes.</summary>
    public const int MaximumDurationMinutes = 8 * 60;

    /// <summary>Builds the rules.</summary>
    public BookAppointmentCommandValidator()
    {
        RuleFor(c => c.PatientId).NotEmpty();
        RuleFor(c => c.ProviderId).NotEmpty();
        RuleFor(c => c.OperatoryId).NotEmpty();
        RuleFor(c => c.Kind).IsInEnum();
        RuleFor(c => c.Notes).MaximumLength(2000);

        RuleFor(c => c.DurationMinutes)
            .InclusiveBetween(MinimumDurationMinutes, MaximumDurationMinutes);

        RuleFor(c => c.StartsAt)
            .NotEqual(default(DateTimeOffset))
            .Must(start => start > DateTimeOffset.UtcNow.AddYears(-1))
            .WithMessage("StartsAt is implausibly far in the past.")
            .Must(start => start < DateTimeOffset.UtcNow.AddYears(3))
            .WithMessage("StartsAt is implausibly far in the future.");
    }
}
