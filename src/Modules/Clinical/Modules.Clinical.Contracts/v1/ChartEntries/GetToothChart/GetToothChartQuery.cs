using Dental.Modules.Clinical.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Clinical.Contracts.v1.ChartEntries.GetToothChart;

/// <summary>Reads the latest finding for every charted tooth of one patient.</summary>
/// <param name="PatientId">Patient identifier.</param>
public sealed record GetToothChartQuery(Guid PatientId) : IQuery<IReadOnlyList<ChartEntryDto>>;
