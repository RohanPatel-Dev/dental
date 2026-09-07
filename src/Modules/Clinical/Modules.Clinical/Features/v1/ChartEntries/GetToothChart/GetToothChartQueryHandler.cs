using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.ChartEntries.GetToothChart;
using Dental.Modules.Clinical.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Clinical.Features.v1.ChartEntries.GetToothChart;

/// <summary>Reads the latest finding for every charted tooth of one patient.</summary>
/// <param name="context">The clinical context.</param>
public sealed class GetToothChartQueryHandler(ClinicalDbContext context)
    : IQueryHandler<GetToothChartQuery, IReadOnlyList<ChartEntryDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<ChartEntryDto>> Handle(
        GetToothChartQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // The chart is append only, so "the chart" is the newest entry per tooth, not every entry.
        List<ChartEntryDto> chart = await context.ChartEntries
            .AsNoTracking()
            .Where(e => e.PatientId == query.PatientId)
            .GroupBy(e => e.ToothNumber)
            .Select(g => g.OrderByDescending(e => e.RecordedAt).First())
            .OrderBy(e => e.ToothNumber)
            .Select(e => new ChartEntryDto(
                e.Id,
                e.PatientId,
                e.ToothNumber,
                e.Surfaces,
                e.Condition,
                e.ProviderId,
                e.RecordedAt,
                e.Notes))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return chart;
    }
}
