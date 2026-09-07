using Dental.Framework.Persistence.Pagination;
using Dental.Framework.Shared.Pagination;
using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Contracts.v1.Invoices.SearchInvoices;
using Dental.Modules.Billing.Data;
using Dental.Modules.Billing.Domain;
using Dental.Modules.Billing.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Billing.Features.v1.Invoices.SearchInvoices;

/// <summary>Pages through invoices.</summary>
/// <param name="context">The billing context.</param>
public sealed class SearchInvoicesQueryHandler(BillingDbContext context)
    : IQueryHandler<SearchInvoicesQuery, PagedResponse<InvoiceDto>>
{
    /// <inheritdoc />
    public async ValueTask<PagedResponse<InvoiceDto>> Handle(
        SearchInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Invoice> invoices = context.Invoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Payments);

        if (query.PatientId is { } patientId)
        {
            invoices = invoices.Where(i => i.PatientId == patientId);
        }

        if (query.Status is { } status)
        {
            invoices = invoices.Where(i => i.Status == status);
        }

        if (query.From is { } from)
        {
            invoices = invoices.Where(i => i.CreatedAt >= from);
        }

        if (query.To is { } to)
        {
            invoices = invoices.Where(i => i.CreatedAt < to);
        }

        invoices = query.Sort == "createdAt asc"
            ? invoices.OrderBy(i => i.CreatedAt)
            : invoices.OrderByDescending(i => i.CreatedAt);

        PagedResponse<Invoice> page = await invoices
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<InvoiceDto>(
            [.. page.Items.Select(InvoiceService.Map)],
            page.PageNumber,
            page.PageSize,
            page.TotalCount);
    }
}
