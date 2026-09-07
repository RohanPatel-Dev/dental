using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Contracts.Services;
using Dental.Modules.Billing.Data;
using Dental.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Billing.Services;

/// <summary>The Billing module's implementation of its own public contract.</summary>
/// <param name="context">The billing context.</param>
public sealed class InvoiceService(BillingDbContext context) : IInvoiceService
{
    /// <inheritdoc />
    public async Task<InvoiceDto?> GetAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        Invoice? invoice = await context.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
            .ConfigureAwait(false);

        return invoice is null ? null : Map(invoice);
    }

    /// <inheritdoc />
    public async Task<decimal> GetOutstandingBalanceAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        // Computed in the database rather than by loading aggregates: a long-standing patient can
        // have hundreds of invoices, and only the number is needed.
        var totals = await context.Invoices
            .AsNoTracking()
            .Where(i => i.PatientId == patientId)
            .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)
            .Select(i => new
            {
                Charged = i.Lines.Sum(l => (decimal?)l.Amount) ?? 0m,
                Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0m,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return totals.Sum(t => t.Charged - t.Paid);
    }

    internal static InvoiceDto Map(Invoice invoice) =>
        new(
            invoice.Id,
            invoice.Number,
            invoice.PatientId,
            invoice.Status,
            [
                .. invoice.Lines.Select(l => new InvoiceLineDto(
                    l.Id,
                    l.ProcedureCode,
                    l.Description,
                    l.ToothNumber,
                    l.Amount)),
            ],
            invoice.Total,
            invoice.AmountPaid,
            invoice.Balance,
            invoice.Currency,
            invoice.IssuedAt,
            invoice.CreatedAt);
}
