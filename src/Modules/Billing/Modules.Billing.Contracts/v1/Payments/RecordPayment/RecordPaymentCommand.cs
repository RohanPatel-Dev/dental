using Dental.Modules.Billing.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Billing.Contracts.v1.Payments.RecordPayment;

/// <summary>Records a payment against an invoice.</summary>
/// <param name="InvoiceId">Invoice to apply it to.</param>
/// <param name="Amount">Amount received.</param>
/// <param name="Method">How it was taken.</param>
/// <param name="Reference">External reference, e.g. a card terminal receipt number.</param>
public sealed record RecordPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    string? Reference) : ICommand<PaymentDto>;
