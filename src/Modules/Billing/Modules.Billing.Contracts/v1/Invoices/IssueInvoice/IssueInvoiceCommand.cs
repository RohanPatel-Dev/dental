using Dental.Modules.Billing.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Billing.Contracts.v1.Invoices.IssueInvoice;

/// <summary>Presents a draft invoice to the patient.</summary>
/// <param name="InvoiceId">Invoice identifier.</param>
public sealed record IssueInvoiceCommand(Guid InvoiceId) : ICommand<InvoiceDto>;
