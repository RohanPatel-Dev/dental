using Dental.Modules.Billing.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Billing.Contracts.v1.Invoices.VoidInvoice;

/// <summary>Cancels an invoice without payment.</summary>
/// <param name="InvoiceId">Invoice identifier.</param>
/// <param name="Reason">Why it is being voided.</param>
public sealed record VoidInvoiceCommand(Guid InvoiceId, string Reason) : ICommand<InvoiceDto>;
