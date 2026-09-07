using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Contracts.Events;
using Dental.Modules.Billing.Contracts.v1.Payments.RecordPayment;
using Dental.Modules.Billing.Data;
using Dental.Modules.Billing.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Billing.Features.v1.Payments.RecordPayment;

/// <summary>Records a payment against an invoice.</summary>
/// <param name="context">The billing context.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="timeProvider">Clock.</param>
public sealed class RecordPaymentCommandHandler(
    BillingDbContext context,
    IOutboxStore outbox,
    TimeProvider timeProvider) : ICommandHandler<RecordPaymentCommand, PaymentDto>
{
    /// <inheritdoc />
    public async ValueTask<PaymentDto> Handle(
        RecordPaymentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Invoice invoice = await context.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Invoice", command.InvoiceId);

        Payment payment = new()
        {
            InvoiceId = invoice.Id,
            Amount = command.Amount,
            Currency = invoice.Currency,
            Method = command.Method,
            Reference = command.Reference,
            ReceivedAt = timeProvider.GetUtcNow(),
            TenantId = invoice.TenantId,
        };

        // The aggregate enforces the currency match and the overpayment check.
        invoice.ApplyPayment(payment);

        if (invoice.Status == InvoiceStatus.Paid)
        {
            await outbox.AddAsync(
                    new InvoiceSettledIntegrationEvent(
                        invoice.Id,
                        invoice.PatientId,
                        invoice.Total,
                        invoice.Currency)
                    {
                        TenantId = invoice.TenantId,
                        Source = nameof(Billing),
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new PaymentDto(
            payment.Id,
            payment.InvoiceId,
            payment.Amount,
            payment.Currency,
            payment.Method,
            payment.Reference,
            payment.ReceivedAt);
    }
}
