namespace Dental.Modules.Scheduling.Contracts.Dtos;

/// <summary>Where an appointment is in its lifecycle.</summary>
public enum AppointmentStatus
{
    /// <summary>Booked but not yet arrived.</summary>
    Scheduled = 0,

    /// <summary>The patient confirmed they are coming.</summary>
    Confirmed = 1,

    /// <summary>The patient has arrived.</summary>
    Arrived = 2,

    /// <summary>Treatment finished.</summary>
    Completed = 3,

    /// <summary>Cancelled by the practice or the patient.</summary>
    Cancelled = 4,

    /// <summary>The patient did not attend.</summary>
    NoShow = 5,
}

/// <summary>What the appointment is for.</summary>
public enum AppointmentKind
{
    /// <summary>Routine examination.</summary>
    Checkup = 0,

    /// <summary>Scale and polish.</summary>
    Hygiene = 1,

    /// <summary>Restorative or surgical treatment.</summary>
    Treatment = 2,

    /// <summary>Unscheduled, seen at short notice.</summary>
    Emergency = 3,

    /// <summary>Follow-up on previous treatment.</summary>
    Review = 4,
}

/// <summary>An appointment.</summary>
/// <param name="Id">Appointment identifier.</param>
/// <param name="PatientId">Patient the appointment is for.</param>
/// <param name="PatientName">Patient display name, resolved through the Patients contract.</param>
/// <param name="ProviderId">Provider delivering the appointment.</param>
/// <param name="ProviderName">Provider display name.</param>
/// <param name="OperatoryId">Chair the appointment occupies.</param>
/// <param name="StartsAt">Start time, in UTC.</param>
/// <param name="EndsAt">End time, in UTC.</param>
/// <param name="Kind">What the appointment is for.</param>
/// <param name="Status">Where it is in its lifecycle.</param>
/// <param name="Notes">Front desk notes. Never clinical detail.</param>
/// <param name="CancellationReason">Why it was cancelled, when it was.</param>
public sealed record AppointmentDto(
    Guid Id,
    Guid PatientId,
    string? PatientName,
    Guid ProviderId,
    string? ProviderName,
    Guid OperatoryId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    AppointmentKind Kind,
    AppointmentStatus Status,
    string? Notes,
    string? CancellationReason)
{
    /// <summary>How long the appointment lasts.</summary>
    public TimeSpan Duration => EndsAt - StartsAt;
}

/// <summary>A clinician who sees patients.</summary>
/// <param name="Id">Provider identifier.</param>
/// <param name="UserId">Linked staff account, when there is one.</param>
/// <param name="DisplayName">Name shown on the appointment book.</param>
/// <param name="Speciality">Their speciality.</param>
/// <param name="IsAcceptingPatients">Whether new patients may be booked with them.</param>
public sealed record ProviderDto(
    Guid Id,
    Guid? UserId,
    string DisplayName,
    string? Speciality,
    bool IsAcceptingPatients);

/// <summary>A treatment room or chair.</summary>
/// <param name="Id">Operatory identifier.</param>
/// <param name="Name">Room name.</param>
/// <param name="IsActive">Whether it can be booked.</param>
public sealed record OperatoryDto(Guid Id, string Name, bool IsActive);
