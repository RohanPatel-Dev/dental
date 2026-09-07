using Dental.Framework.Core.Domain;
using Dental.Modules.Clinical.Contracts.Dtos;

namespace Dental.Modules.Clinical.Domain;

/// <summary>One recorded finding against a tooth. Append only - a correction is a new entry.</summary>
public sealed class ChartEntry : BaseEntity, IAuditableEntity
{
    /// <summary>Patient the finding is for.</summary>
    public Guid PatientId { get; set; }

    /// <summary>FDI tooth number: 11-18, 21-28, 31-38, 41-48 for adults.</summary>
    public int ToothNumber { get; set; }

    /// <summary>Surfaces the finding applies to, e.g. <c>MOD</c>.</summary>
    public string? Surfaces { get; set; }

    /// <summary>The condition observed.</summary>
    public ToothCondition Condition { get; set; }

    /// <summary>Provider who recorded it.</summary>
    public Guid ProviderId { get; set; }

    /// <summary>When it was recorded.</summary>
    public DateTimeOffset RecordedAt { get; set; }

    /// <summary>Clinical notes. Health data.</summary>
    public string? Notes { get; set; }

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }

    /// <summary>Removes the free-text note when a patient's record is erased.</summary>
    public void Erase() => Notes = null;
}
