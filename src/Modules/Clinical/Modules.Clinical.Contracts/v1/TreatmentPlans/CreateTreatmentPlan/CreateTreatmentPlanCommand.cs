using Dental.Modules.Clinical.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.CreateTreatmentPlan;

/// <summary>Authors a treatment plan for a patient.</summary>
/// <param name="PatientId">Patient it is for.</param>
/// <param name="ProviderId">Provider authoring it.</param>
/// <param name="Items">The planned items.</param>
/// <param name="Notes">Clinical notes.</param>
public sealed record CreateTreatmentPlanCommand(
    Guid PatientId,
    Guid ProviderId,
    IReadOnlyList<TreatmentPlanItemInput> Items,
    string? Notes) : ICommand<TreatmentPlanDto>;

/// <summary>One requested line of a new treatment plan.</summary>
/// <param name="ProcedureId">Catalogued procedure.</param>
/// <param name="ToothNumber">FDI tooth number, when the item applies to one tooth.</param>
/// <param name="Surfaces">Tooth surfaces treated, e.g. <c>MOD</c>.</param>
/// <param name="Fee">Fee to quote. Falls back to the catalog fee when omitted.</param>
public sealed record TreatmentPlanItemInput(
    Guid ProcedureId,
    int? ToothNumber,
    string? Surfaces,
    decimal? Fee);
