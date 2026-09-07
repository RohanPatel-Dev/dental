using Dental.Framework.Core.Domain;
using Dental.Modules.Patients.Contracts.Dtos;

namespace Dental.Modules.Patients.Domain;

/// <summary>
/// A patient of the practice. Tenant scoped, soft deletable and auditable - every field on it is
/// personal data, and several fields are special category health data.
/// </summary>
public sealed class Patient : AggregateRoot, IAuditableEntity, ISoftDeletable
{
    /// <summary>Practice-visible chart number, unique within the tenant.</summary>
    public string ChartNumber { get; set; } = default!;

    /// <summary>Given name.</summary>
    public string FirstName { get; set; } = default!;

    /// <summary>Family name.</summary>
    public string LastName { get; set; } = default!;

    /// <summary>Date of birth.</summary>
    public DateOnly DateOfBirth { get; set; }

    /// <summary>Recorded sex.</summary>
    public PatientSex Sex { get; set; }

    /// <summary>Contact address.</summary>
    public string? Email { get; set; }

    /// <summary>Contact number.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Record status.</summary>
    public PatientStatus Status { get; set; } = PatientStatus.Active;

    /// <summary>Provider the patient normally sees, when they have one.</summary>
    public Guid? PreferredProviderId { get; set; }

    /// <summary>Recorded allergies. Health data - never logged, never exported casually.</summary>
    public List<string> Allergies { get; set; } = [];

    /// <summary>Whether the patient agreed to marketing contact.</summary>
    public bool HasMarketingConsent { get; set; }

    /// <summary>Whether the patient agreed to appointment reminders.</summary>
    public bool HasReminderConsent { get; set; }

    /// <summary>When consent was last recorded.</summary>
    public DateTimeOffset? ConsentRecordedAt { get; set; }

    /// <summary>Set once the record has been through erasure, so it cannot be revived.</summary>
    public bool IsErased { get; set; }

    /// <summary>Documents attached to the record.</summary>
    public List<PatientDocument> Documents { get; } = [];

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; set; }

    /// <summary>Display name.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>Records a change to contact consent.</summary>
    /// <param name="marketing">Whether marketing contact is permitted.</param>
    /// <param name="reminders">Whether appointment reminders are permitted.</param>
    /// <param name="now">Current time.</param>
    public void RecordConsent(bool marketing, bool reminders, DateTimeOffset now)
    {
        HasMarketingConsent = marketing;
        HasReminderConsent = reminders;
        ConsentRecordedAt = now;

        RaiseDomainEvent(new PatientConsentChangedDomainEvent(Id, marketing, reminders));
    }

    /// <summary>
    /// Overwrites every identifying field with a placeholder, leaving the row so that clinical
    /// history and financial records keep referential integrity.
    /// </summary>
    /// <param name="now">Current time.</param>
    /// <remarks>
    /// Pseudonymisation rather than deletion: a dental practice has a statutory duty to retain
    /// treatment records, so the lawful response to an erasure request is to remove the identifiers
    /// and keep the clinical facts.
    /// </remarks>
    public void Erase(DateTimeOffset now)
    {
        FirstName = "Erased";
        LastName = "Patient";
        Email = null;
        PhoneNumber = null;
        DateOfBirth = DateOnly.MinValue;
        Sex = PatientSex.Unknown;
        Allergies.Clear();
        HasMarketingConsent = false;
        HasReminderConsent = false;
        Status = PatientStatus.Inactive;
        IsErased = true;
        DeletedAt = now;
        Documents.Clear();
    }
}

/// <summary>Raised in-process when a patient's consent changes.</summary>
/// <param name="PatientId">The patient.</param>
/// <param name="HasMarketingConsent">Whether marketing contact is now permitted.</param>
/// <param name="HasReminderConsent">Whether appointment reminders are now permitted.</param>
public sealed record PatientConsentChangedDomainEvent(
    Guid PatientId,
    bool HasMarketingConsent,
    bool HasReminderConsent) : DomainEvent;
