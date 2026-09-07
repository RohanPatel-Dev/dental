namespace Dental.Modules.Scheduling;

/// <summary>
/// Realtime event names this module broadcasts. The SPAs pre-register these in their realtime
/// provider, so the two lists have to agree.
/// </summary>
public static class RealtimeEvents
{
    /// <summary>An appointment was added to the book.</summary>
    public const string AppointmentBooked = "appointment.booked";

    /// <summary>An appointment moved to a new slot.</summary>
    public const string AppointmentRescheduled = "appointment.rescheduled";

    /// <summary>An appointment was cancelled or marked a no-show.</summary>
    public const string AppointmentCancelled = "appointment.cancelled";

    /// <summary>Treatment finished.</summary>
    public const string AppointmentCompleted = "appointment.completed";
}
