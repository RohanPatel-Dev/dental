using Dental.Framework.Core.Domain;
using Dental.Framework.Core.Exceptions;
using Dental.Framework.Core.ValueObjects;
using Dental.Modules.Scheduling.Contracts.Dtos;

namespace Dental.Modules.Scheduling.Domain;

/// <summary>One booked slot in the appointment book.</summary>
public sealed class Appointment : AggregateRoot, IAuditableEntity, ISoftDeletable
{
    /// <summary>Patient the appointment is for.</summary>
    public Guid PatientId { get; set; }

    /// <summary>Provider delivering it.</summary>
    public Guid ProviderId { get; set; }

    /// <summary>Chair it occupies.</summary>
    public Guid OperatoryId { get; set; }

    /// <summary>Start time, in UTC.</summary>
    public DateTimeOffset StartsAt { get; set; }

    /// <summary>End time, in UTC.</summary>
    public DateTimeOffset EndsAt { get; set; }

    /// <summary>What the appointment is for.</summary>
    public AppointmentKind Kind { get; set; }

    /// <summary>Where it is in its lifecycle.</summary>
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    /// <summary>Front desk notes. Clinical detail belongs in the Clinical module, not here.</summary>
    public string? Notes { get; set; }

    /// <summary>Why it was cancelled, when it was.</summary>
    public string? CancellationReason { get; set; }

    /// <summary>When treatment finished.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; set; }

    /// <summary>The slot this appointment occupies.</summary>
    public DateRange Slot => new(StartsAt, EndsAt);

    /// <summary>True while the appointment still occupies its slot.</summary>
    public bool OccupiesSlot =>
        Status is not (AppointmentStatus.Cancelled or AppointmentStatus.NoShow);

    /// <summary>Moves the appointment to a new slot.</summary>
    /// <param name="startsAt">New start time.</param>
    /// <param name="duration">New duration.</param>
    /// <param name="operatoryId">New chair, when it changes.</param>
    /// <exception cref="ConflictException">The appointment is no longer reschedulable.</exception>
    public void Reschedule(DateTimeOffset startsAt, TimeSpan duration, Guid? operatoryId)
    {
        if (Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
        {
            throw new ConflictException(
                $"An appointment that is {Status} cannot be rescheduled.");
        }

        StartsAt = startsAt;
        EndsAt = startsAt.Add(duration);

        if (operatoryId is { } operatory)
        {
            OperatoryId = operatory;
        }

        Status = AppointmentStatus.Scheduled;
    }

    /// <summary>Cancels the appointment or records a non-attendance.</summary>
    /// <param name="reason">Why it is being cancelled.</param>
    /// <param name="isNoShow">True when the patient simply did not attend.</param>
    /// <exception cref="ConflictException">The appointment has already been completed.</exception>
    public void Cancel(string reason, bool isNoShow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status == AppointmentStatus.Completed)
        {
            throw new ConflictException("A completed appointment cannot be cancelled.");
        }

        Status = isNoShow ? AppointmentStatus.NoShow : AppointmentStatus.Cancelled;
        CancellationReason = reason;
    }

    /// <summary>Marks treatment complete.</summary>
    /// <param name="completedAt">When treatment finished.</param>
    /// <exception cref="ConflictException">The appointment is not in a completable state.</exception>
    public void Complete(DateTimeOffset completedAt)
    {
        if (Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
        {
            throw new ConflictException($"An appointment that is {Status} cannot be completed.");
        }

        if (Status == AppointmentStatus.Completed)
        {
            throw new ConflictException("This appointment is already complete.");
        }

        Status = AppointmentStatus.Completed;
        CompletedAt = completedAt;
    }

    /// <summary>Erases the free-text fields that could carry personal data.</summary>
    public void Erase()
    {
        Notes = null;
        CancellationReason = null;
    }
}
