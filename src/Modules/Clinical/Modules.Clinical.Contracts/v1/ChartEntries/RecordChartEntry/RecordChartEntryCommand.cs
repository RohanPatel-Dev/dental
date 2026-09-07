using Dental.Modules.Clinical.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Clinical.Contracts.v1.ChartEntries.RecordChartEntry;

/// <summary>Records a finding against one tooth.</summary>
/// <param name="PatientId">Patient the finding is for.</param>
/// <param name="ProviderId">Provider recording it.</param>
/// <param name="ToothNumber">FDI tooth number.</param>
/// <param name="Surfaces">Surfaces the finding applies to.</param>
/// <param name="Condition">The condition observed.</param>
/// <param name="Notes">Clinical notes.</param>
public sealed record RecordChartEntryCommand(
    Guid PatientId,
    Guid ProviderId,
    int ToothNumber,
    string? Surfaces,
    ToothCondition Condition,
    string? Notes) : ICommand<ChartEntryDto>;
