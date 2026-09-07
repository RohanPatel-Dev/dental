using Dental.Framework.Core.Exceptions;
using Dental.Modules.Scheduling.Contracts.Dtos;
using Dental.Modules.Scheduling.Domain;
using Shouldly;

namespace Dental.Modules.Scheduling.Tests.Domain;

/// <summary>The appointment lifecycle rules, which the aggregate owns rather than a handler.</summary>
public sealed class AppointmentTests
{
    #region Happy Path

    [Fact]
    public void Reschedule_Should_MoveTheSlotAndReturnToScheduled_When_TheAppointmentIsLive()
    {
        // Arrange
        Appointment appointment = Booked();
        DateTimeOffset newStart = appointment.StartsAt.AddDays(1);

        // Act
        appointment.Reschedule(newStart, TimeSpan.FromMinutes(30), operatoryId: null);

        // Assert
        appointment.StartsAt.ShouldBe(newStart);
        appointment.EndsAt.ShouldBe(newStart.AddMinutes(30));
        appointment.Status.ShouldBe(AppointmentStatus.Scheduled);
    }

    [Fact]
    public void Reschedule_Should_MoveTheChair_When_AnOperatoryIsSupplied()
    {
        Appointment appointment = Booked();
        Guid newOperatory = Guid.CreateVersion7();

        appointment.Reschedule(appointment.StartsAt, TimeSpan.FromMinutes(30), newOperatory);

        appointment.OperatoryId.ShouldBe(newOperatory);
    }

    [Fact]
    public void Complete_Should_RecordTheCompletionTime()
    {
        Appointment appointment = Booked();
        DateTimeOffset completedAt = DateTimeOffset.UtcNow;

        appointment.Complete(completedAt);

        appointment.Status.ShouldBe(AppointmentStatus.Completed);
        appointment.CompletedAt.ShouldBe(completedAt);
    }

    [Fact]
    public void Cancel_Should_RecordANoShow_Separately_From_ACancellation()
    {
        Appointment cancelled = Booked();
        Appointment noShow = Booked();

        cancelled.Cancel("Patient rebooked.", isNoShow: false);
        noShow.Cancel("Did not attend.", isNoShow: true);

        cancelled.Status.ShouldBe(AppointmentStatus.Cancelled);
        noShow.Status.ShouldBe(AppointmentStatus.NoShow);
    }

    #endregion

    #region Exception Cases

    [Fact]
    public void Reschedule_Should_Throw_When_TheAppointmentIsAlreadyComplete()
    {
        Appointment appointment = Booked();
        appointment.Complete(DateTimeOffset.UtcNow);

        Should.Throw<ConflictException>(() =>
            appointment.Reschedule(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(30), null));
    }

    [Fact]
    public void Cancel_Should_Throw_When_TheAppointmentIsAlreadyComplete()
    {
        Appointment appointment = Booked();
        appointment.Complete(DateTimeOffset.UtcNow);

        Should.Throw<ConflictException>(() => appointment.Cancel("Too late.", isNoShow: false));
    }

    [Fact]
    public void Complete_Should_Throw_When_TheAppointmentWasCancelled()
    {
        Appointment appointment = Booked();
        appointment.Cancel("Patient rebooked.", isNoShow: false);

        Should.Throw<ConflictException>(() => appointment.Complete(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Complete_Should_Throw_When_CalledTwice()
    {
        // Completion is what triggers billing, so completing twice would charge the patient twice.
        Appointment appointment = Booked();
        appointment.Complete(DateTimeOffset.UtcNow);

        Should.Throw<ConflictException>(() => appointment.Complete(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Cancel_Should_Throw_When_NoReasonIsGiven()
    {
        Appointment appointment = Booked();

        Should.Throw<ArgumentException>(() => appointment.Cancel("  ", isNoShow: false));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void OccupiesSlot_Should_BeFalse_Once_TheAppointmentIsCancelledOrANoShow()
    {
        // This is what lets a cancelled slot be rebooked; the conflict check relies on it.
        Appointment cancelled = Booked();
        Appointment noShow = Booked();
        Appointment completed = Booked();

        cancelled.Cancel("Patient rebooked.", isNoShow: false);
        noShow.Cancel("Did not attend.", isNoShow: true);
        completed.Complete(DateTimeOffset.UtcNow);

        cancelled.OccupiesSlot.ShouldBeFalse();
        noShow.OccupiesSlot.ShouldBeFalse();
        completed.OccupiesSlot.ShouldBeTrue();
    }

    [Fact]
    public void Slot_Should_BeHalfOpen_So_BackToBackAppointmentsDoNotClash()
    {
        DateTimeOffset start = new(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);

        Appointment first = Booked(start, TimeSpan.FromMinutes(30));
        Appointment second = Booked(start.AddMinutes(30), TimeSpan.FromMinutes(30));

        first.Slot.Overlaps(second.Slot).ShouldBeFalse(
            "A 09:00-09:30 and a 09:30-10:00 appointment are back to back, not a double booking.");
    }

    [Fact]
    public void Erase_Should_ClearTheFreeTextFields_But_KeepTheAttendanceRecord()
    {
        Appointment appointment = Booked();
        appointment.Notes = "Patient mentioned their address changed.";
        appointment.Cancel("Called to say they had moved away.", isNoShow: false);

        appointment.Erase();

        appointment.Notes.ShouldBeNull();
        appointment.CancellationReason.ShouldBeNull();
        appointment.Status.ShouldBe(AppointmentStatus.Cancelled);
        appointment.StartsAt.ShouldNotBe(default);
    }

    #endregion

    private static Appointment Booked(DateTimeOffset? startsAt = null, TimeSpan? duration = null)
    {
        DateTimeOffset start = startsAt ?? new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);
        TimeSpan length = duration ?? TimeSpan.FromMinutes(45);

        return new Appointment
        {
            PatientId = Guid.CreateVersion7(),
            ProviderId = Guid.CreateVersion7(),
            OperatoryId = Guid.CreateVersion7(),
            StartsAt = start,
            EndsAt = start.Add(length),
            Kind = AppointmentKind.Checkup,
            Status = AppointmentStatus.Scheduled,
            TenantId = "root",
        };
    }
}
