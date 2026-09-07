using Dental.Framework.Core.Domain;

namespace Dental.Modules.Notifications.Domain;

/// <summary>
/// This module's own copy of the few patient facts it needs to address a message.
/// </summary>
/// <remarks>
/// <para>
/// A local read model, not a foreign key. The Notifications module runs in its own process, where
/// <c>IPatientService</c> does not exist and the Patients schema is not reachable - so it maintains
/// this projection from <c>PatientRegistered</c>, <c>PatientContactChanged</c>,
/// <c>PatientConsentChanged</c> and <c>PatientErasureRequested</c>.
/// </para>
/// <para>
/// This is the real cost of extracting a module into its own host: a projection to keep current, and
/// a backfill to run when the host is first deployed against an existing database. Do not extract a
/// module until independent scaling actually pays for that.
/// </para>
/// </remarks>
public sealed class PatientContact : BaseEntity
{
    /// <summary>Identifier of the patient in the Patients module.</summary>
    public Guid PatientId { get; set; }

    /// <summary>Display name, as of the last event applied.</summary>
    public string FullName { get; set; } = default!;

    /// <summary>Contact address, as of the last event applied.</summary>
    public string? Email { get; set; }

    /// <summary>Contact number, as of the last event applied.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Whether appointment reminders are permitted.</summary>
    public bool HasReminderConsent { get; set; }

    /// <summary>Set once the patient has been erased, so nothing further is ever addressed to them.</summary>
    public bool IsErased { get; set; }

    /// <summary>True when this contact may be sent a message.</summary>
    public bool IsContactable =>
        !IsErased && HasReminderConsent && !string.IsNullOrWhiteSpace(Email);

    /// <summary>Clears every identifying field.</summary>
    public void Erase()
    {
        FullName = "Erased patient";
        Email = null;
        PhoneNumber = null;
        HasReminderConsent = false;
        IsErased = true;
    }
}
