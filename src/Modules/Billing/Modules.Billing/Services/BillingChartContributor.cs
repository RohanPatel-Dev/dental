using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Data;
using Dental.Modules.Clinical.Contracts.Services;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Billing.Services;

/// <summary>
/// Adds this module's invoices to a patient's clinical timeline.
/// </summary>
/// <remarks>
/// The contributor pattern in action: Clinical owns <see cref="IChartContributor"/> and fans out to
/// whatever is registered, so it needs no reference to Billing. Adding a new module to the timeline
/// later costs one registration and no change to Clinical at all.
/// </remarks>
/// <param name="context">The billing context.</param>
public sealed class BillingChartContributor(BillingDbContext context) : IChartContributor
{
    /// <inheritdoc />
    public int Order => 900;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChartTimelineEntry>> GetEntriesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        // Projected to a flat shape in SQL, then formatted in memory: string casing and
        // interpolation do not belong in an expression tree.
        var rows = await context.Invoices
            .AsNoTracking()
            .Where(i => i.PatientId == patientId && i.Status != InvoiceStatus.Draft)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                OccurredAt = i.IssuedAt ?? i.CreatedAt,
                i.Status,
                i.Number,
                i.Currency,
                i.Id,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return
        [
            .. rows.Select(r => new ChartTimelineEntry(
                r.OccurredAt,
                "Billing",
                $"invoice.{r.Status.ToString().ToLowerInvariant()}",
                $"Invoice {r.Number} ({r.Currency})",
                r.Id)),
        ];
    }
}
