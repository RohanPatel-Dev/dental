using System.Globalization;
using Dental.Modules.Billing.Data;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Billing.Services;

/// <summary>Allocates the next practice-visible invoice number.</summary>
/// <param name="context">The billing context.</param>
/// <param name="timeProvider">Clock, so the year prefix is testable.</param>
public sealed class InvoiceNumberGenerator(BillingDbContext context, TimeProvider timeProvider)
{
    /// <summary>Produces the next invoice number, of the form <c>INV-2026-000123</c>.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The invoice number.</returns>
    /// <remarks>
    /// The unique index on (TenantId, Number) is the real guarantee; a concurrent duplicate loses
    /// the insert and is retried by the caller.
    /// </remarks>
    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        int year = timeProvider.GetUtcNow().Year;
        string prefix = string.Create(CultureInfo.InvariantCulture, $"INV-{year}-");

        int issuedThisYear = await context.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.TenantId == context.CurrentTenantId && i.Number.StartsWith(prefix))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        return string.Create(CultureInfo.InvariantCulture, $"{prefix}{issuedThisYear + 1:D6}");
    }
}
