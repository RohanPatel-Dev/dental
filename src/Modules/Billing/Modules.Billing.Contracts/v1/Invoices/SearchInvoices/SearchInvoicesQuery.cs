using Dental.Framework.Shared.Pagination;
using Dental.Modules.Billing.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Billing.Contracts.v1.Invoices.SearchInvoices;

/// <summary>Pages through invoices.</summary>
/// <param name="PatientId">Filters to one patient.</param>
/// <param name="Status">Filters by lifecycle status.</param>
/// <param name="From">Earliest creation time.</param>
/// <param name="To">Latest creation time.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchInvoicesQuery(
    Guid? PatientId,
    InvoiceStatus? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int PageNumber,
    int PageSize,
    string? Sort) : IQuery<PagedResponse<InvoiceDto>>, IPagedQuery;
