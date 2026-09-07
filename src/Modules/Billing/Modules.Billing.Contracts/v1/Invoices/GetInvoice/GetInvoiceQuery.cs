using Dental.Modules.Billing.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Billing.Contracts.v1.Invoices.GetInvoice;

/// <summary>Reads one invoice with its lines.</summary>
/// <param name="InvoiceId">Invoice identifier.</param>
public sealed record GetInvoiceQuery(Guid InvoiceId) : IQuery<InvoiceDto>;
