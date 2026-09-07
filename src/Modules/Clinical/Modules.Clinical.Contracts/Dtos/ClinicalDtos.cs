namespace Dental.Modules.Clinical.Contracts.Dtos;

/// <summary>Broad category of a catalogued procedure.</summary>
public enum ProcedureCategory
{
    /// <summary>Examinations and radiographs.</summary>
    Diagnostic = 0,

    /// <summary>Cleaning, fluoride, sealants.</summary>
    Preventive = 1,

    /// <summary>Fillings, crowns, inlays.</summary>
    Restorative = 2,

    /// <summary>Root canal treatment.</summary>
    Endodontic = 3,

    /// <summary>Gum treatment.</summary>
    Periodontic = 4,

    /// <summary>Extractions and surgery.</summary>
    Surgical = 5,

    /// <summary>Braces and aligners.</summary>
    Orthodontic = 6,
}

/// <summary>Where a treatment plan is in its lifecycle.</summary>
public enum TreatmentPlanStatus
{
    /// <summary>Being authored.</summary>
    Draft = 0,

    /// <summary>Presented to the patient.</summary>
    Proposed = 1,

    /// <summary>The patient agreed to it.</summary>
    Accepted = 2,

    /// <summary>The patient declined it.</summary>
    Declined = 3,

    /// <summary>All its items have been delivered.</summary>
    Completed = 4,
}

/// <summary>The condition recorded against one tooth surface.</summary>
public enum ToothCondition
{
    /// <summary>No finding.</summary>
    Healthy = 0,

    /// <summary>Decay present.</summary>
    Caries = 1,

    /// <summary>Existing restoration.</summary>
    Restored = 2,

    /// <summary>Crowned.</summary>
    Crowned = 3,

    /// <summary>Root filled.</summary>
    RootFilled = 4,

    /// <summary>Absent.</summary>
    Missing = 5,

    /// <summary>Replaced by an implant.</summary>
    Implant = 6,
}

/// <summary>A catalogued procedure with its default fee.</summary>
/// <param name="Id">Procedure identifier.</param>
/// <param name="Code">Procedure code, e.g. an ADA CDT code.</param>
/// <param name="Description">What the procedure is.</param>
/// <param name="Category">Broad category.</param>
/// <param name="DefaultFee">Default fee charged.</param>
/// <param name="Currency">ISO 4217 currency of the fee.</param>
/// <param name="DefaultDurationMinutes">Chair time normally booked for it.</param>
/// <param name="IsActive">Whether it can still be planned.</param>
public sealed record ProcedureDto(
    Guid Id,
    string Code,
    string Description,
    ProcedureCategory Category,
    decimal DefaultFee,
    string Currency,
    int DefaultDurationMinutes,
    bool IsActive);

/// <summary>One line of a treatment plan.</summary>
/// <param name="Id">Item identifier.</param>
/// <param name="ProcedureId">Catalogued procedure.</param>
/// <param name="ProcedureCode">Procedure code, denormalized for display.</param>
/// <param name="ToothNumber">FDI tooth number, when the item applies to one tooth.</param>
/// <param name="Surfaces">Tooth surfaces treated, e.g. <c>MOD</c>.</param>
/// <param name="Fee">Fee quoted for this item.</param>
/// <param name="IsDelivered">Whether the work has been done.</param>
public sealed record TreatmentPlanItemDto(
    Guid Id,
    Guid ProcedureId,
    string ProcedureCode,
    int? ToothNumber,
    string? Surfaces,
    decimal Fee,
    bool IsDelivered);

/// <summary>A proposed course of treatment.</summary>
/// <param name="Id">Plan identifier.</param>
/// <param name="PatientId">Patient it is for.</param>
/// <param name="ProviderId">Provider who authored it.</param>
/// <param name="Status">Where it is in its lifecycle.</param>
/// <param name="Items">The planned items.</param>
/// <param name="TotalFee">Sum of the item fees.</param>
/// <param name="Currency">ISO 4217 currency of the fees.</param>
/// <param name="Notes">Clinical notes. Health data.</param>
/// <param name="CreatedAt">When the plan was authored.</param>
/// <param name="AcceptedAt">When the patient accepted it.</param>
public sealed record TreatmentPlanDto(
    Guid Id,
    Guid PatientId,
    Guid ProviderId,
    TreatmentPlanStatus Status,
    IReadOnlyList<TreatmentPlanItemDto> Items,
    decimal TotalFee,
    string Currency,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AcceptedAt);

/// <summary>One recorded finding against a tooth.</summary>
/// <param name="Id">Entry identifier.</param>
/// <param name="PatientId">Patient the finding is for.</param>
/// <param name="ToothNumber">FDI tooth number.</param>
/// <param name="Surfaces">Surfaces the finding applies to.</param>
/// <param name="Condition">The condition recorded.</param>
/// <param name="ProviderId">Provider who recorded it.</param>
/// <param name="RecordedAt">When it was recorded.</param>
/// <param name="Notes">Clinical notes. Health data.</param>
public sealed record ChartEntryDto(
    Guid Id,
    Guid PatientId,
    int ToothNumber,
    string? Surfaces,
    ToothCondition Condition,
    Guid ProviderId,
    DateTimeOffset RecordedAt,
    string? Notes);
