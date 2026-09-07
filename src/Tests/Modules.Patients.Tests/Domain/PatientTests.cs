using Dental.Modules.Patients.Contracts.Dtos;
using Dental.Modules.Patients.Domain;
using Shouldly;

namespace Dental.Modules.Patients.Tests.Domain;

/// <summary>Consent and erasure behaviour, which carry legal weight rather than just business rules.</summary>
public sealed class PatientTests
{
    #region Happy Path

    [Fact]
    public void RecordConsent_Should_StoreBothFlagsAndTheTimestamp()
    {
        Patient patient = Registered();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        patient.RecordConsent(marketing: true, reminders: false, now);

        patient.HasMarketingConsent.ShouldBeTrue();
        patient.HasReminderConsent.ShouldBeFalse();
        patient.ConsentRecordedAt.ShouldBe(now);
    }

    [Fact]
    public void FullName_Should_JoinTheNameParts()
    {
        Patient patient = Registered();

        patient.FullName.ShouldBe("Ada Whitfield");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Erase_Should_RemoveEveryIdentifyingField()
    {
        Patient patient = Registered();
        patient.Allergies.Add("Penicillin");
        patient.Documents.Add(new PatientDocument
        {
            PatientId = patient.Id,
            FileName = "consent.pdf",
            StorageKey = "tenants/root/document/consent.pdf",
            TenantId = "root",
        });

        patient.Erase(DateTimeOffset.UtcNow);

        patient.FirstName.ShouldBe("Erased");
        patient.LastName.ShouldBe("Patient");
        patient.Email.ShouldBeNull();
        patient.PhoneNumber.ShouldBeNull();
        patient.DateOfBirth.ShouldBe(DateOnly.MinValue);
        patient.Sex.ShouldBe(PatientSex.Unknown);
        patient.Allergies.ShouldBeEmpty();
        patient.Documents.ShouldBeEmpty();
    }

    [Fact]
    public void Erase_Should_WithdrawEveryConsent()
    {
        // An erased patient must not remain contactable through a flag nobody thought to clear.
        Patient patient = Registered();
        patient.RecordConsent(marketing: true, reminders: true, DateTimeOffset.UtcNow);

        patient.Erase(DateTimeOffset.UtcNow);

        patient.HasMarketingConsent.ShouldBeFalse();
        patient.HasReminderConsent.ShouldBeFalse();
    }

    [Fact]
    public void Erase_Should_KeepTheRow_So_ClinicalAndFinancialHistoryStaysReferential()
    {
        // Pseudonymisation, not deletion: a practice must retain treatment records, so the row
        // survives with its identifiers stripped and its key intact.
        Patient patient = Registered();
        Guid id = patient.Id;
        string chartNumber = patient.ChartNumber;

        patient.Erase(DateTimeOffset.UtcNow);

        patient.Id.ShouldBe(id);
        patient.ChartNumber.ShouldBe(chartNumber);
        patient.IsErased.ShouldBeTrue();
        patient.DeletedAt.ShouldNotBeNull();
        patient.Status.ShouldBe(PatientStatus.Inactive);
    }

    [Fact]
    public void Erase_Should_BeIdempotent()
    {
        Patient patient = Registered();
        DateTimeOffset first = DateTimeOffset.UtcNow;

        patient.Erase(first);
        patient.Erase(first.AddDays(1));

        patient.IsErased.ShouldBeTrue();
        patient.FirstName.ShouldBe("Erased");
    }

    #endregion

    private static Patient Registered() => new()
    {
        ChartNumber = "P-000001",
        FirstName = "Ada",
        LastName = "Whitfield",
        DateOfBirth = new DateOnly(1984, 3, 14),
        Sex = PatientSex.Female,
        Email = "ada@example.test",
        PhoneNumber = "+15551234567",
        Status = PatientStatus.Active,
        TenantId = "root",
    };
}
