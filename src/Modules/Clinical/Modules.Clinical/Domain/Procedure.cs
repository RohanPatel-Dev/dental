using Dental.Framework.Core.Domain;
using Dental.Modules.Clinical.Contracts.Dtos;

namespace Dental.Modules.Clinical.Domain;

/// <summary>A catalogued procedure with its default fee and chair time.</summary>
public sealed class Procedure : BaseEntity, IAuditableEntity, ISoftDeletable
{
    /// <summary>Procedure code, e.g. an ADA CDT code. Unique within the tenant.</summary>
    public string Code { get; set; } = default!;

    /// <summary>What the procedure is.</summary>
    public string Description { get; set; } = default!;

    /// <summary>Broad category.</summary>
    public ProcedureCategory Category { get; set; }

    /// <summary>Default fee charged.</summary>
    public decimal DefaultFee { get; set; }

    /// <summary>ISO 4217 currency of the fee.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Chair time normally booked for it.</summary>
    public int DefaultDurationMinutes { get; set; }

    /// <summary>Whether it can still be planned.</summary>
    public bool IsActive { get; set; } = true;

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; set; }
}
