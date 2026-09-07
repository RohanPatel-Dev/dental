using Dental.Framework.Core.Domain;
using Dental.Framework.Core.Exceptions;
using Dental.Modules.Clinical.Contracts.Dtos;

namespace Dental.Modules.Clinical.Domain;

/// <summary>A proposed course of treatment and the items that make it up.</summary>
public sealed class TreatmentPlan : AggregateRoot, IAuditableEntity, ISoftDeletable
{
    /// <summary>Patient it is for.</summary>
    public Guid PatientId { get; set; }

    /// <summary>Provider who authored it.</summary>
    public Guid ProviderId { get; set; }

    /// <summary>Where it is in its lifecycle.</summary>
    public TreatmentPlanStatus Status { get; set; } = TreatmentPlanStatus.Draft;

    /// <summary>Clinical notes. Health data - redacted from the audit trail and never logged.</summary>
    public string? Notes { get; set; }

    /// <summary>ISO 4217 currency the items are quoted in.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>When the patient accepted it.</summary>
    public DateTimeOffset? AcceptedAt { get; set; }

    /// <summary>The planned items.</summary>
    public List<TreatmentPlanItem> Items { get; } = [];

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; set; }

    /// <summary>Sum of the item fees.</summary>
    public decimal TotalFee => Items.Sum(i => i.Fee);

    /// <summary>Records the patient's acceptance.</summary>
    /// <param name="acceptedAt">When they accepted.</param>
    /// <exception cref="ConflictException">The plan is not in an acceptable state.</exception>
    public void Accept(DateTimeOffset acceptedAt)
    {
        if (Status is TreatmentPlanStatus.Accepted or TreatmentPlanStatus.Completed)
        {
            throw new ConflictException("This treatment plan has already been accepted.");
        }

        if (Status == TreatmentPlanStatus.Declined)
        {
            throw new ConflictException("A declined treatment plan cannot be accepted.");
        }

        Status = TreatmentPlanStatus.Accepted;
        AcceptedAt = acceptedAt;

        RaiseDomainEvent(new TreatmentPlanAcceptedDomainEvent(Id, PatientId, TotalFee, Currency));
    }

    /// <summary>Marks the plan complete once every item has been delivered.</summary>
    public void CompleteIfFullyDelivered()
    {
        if (Items.Count > 0 && Items.TrueForAll(i => i.IsDelivered))
        {
            Status = TreatmentPlanStatus.Completed;
        }
    }

    /// <summary>Removes the free-text clinical notes when a patient's record is erased.</summary>
    public void Erase() => Notes = null;
}

/// <summary>One line of a treatment plan.</summary>
/// <remarks>
/// Reached only through <see cref="TreatmentPlan.Items"/>, so its key is
/// <c>ValueGeneratedNever()</c> - otherwise EF marks a new item Modified rather than Added.
/// </remarks>
public sealed class TreatmentPlanItem : BaseEntity
{
    /// <summary>Owning plan.</summary>
    public Guid TreatmentPlanId { get; set; }

    /// <summary>Catalogued procedure.</summary>
    public Guid ProcedureId { get; set; }

    /// <summary>Procedure code, denormalized so a later catalog edit does not rewrite history.</summary>
    public string ProcedureCode { get; set; } = default!;

    /// <summary>What was planned, denormalized for the same reason.</summary>
    public string Description { get; set; } = default!;

    /// <summary>FDI tooth number, when the item applies to one tooth.</summary>
    public int? ToothNumber { get; set; }

    /// <summary>Tooth surfaces treated, e.g. <c>MOD</c>.</summary>
    public string? Surfaces { get; set; }

    /// <summary>Fee quoted for this item.</summary>
    public decimal Fee { get; set; }

    /// <summary>Whether the work has been done.</summary>
    public bool IsDelivered { get; set; }

    /// <summary>Appointment the work was delivered at.</summary>
    public Guid? DeliveredAtAppointmentId { get; set; }

    /// <summary>When the work was delivered.</summary>
    public DateTimeOffset? DeliveredAt { get; set; }
}

/// <summary>Raised in-process when a patient accepts a plan.</summary>
/// <param name="TreatmentPlanId">The accepted plan.</param>
/// <param name="PatientId">Patient who accepted it.</param>
/// <param name="TotalFee">Total quoted.</param>
/// <param name="Currency">ISO 4217 currency of the quote.</param>
public sealed record TreatmentPlanAcceptedDomainEvent(
    Guid TreatmentPlanId,
    Guid PatientId,
    decimal TotalFee,
    string Currency) : DomainEvent;
