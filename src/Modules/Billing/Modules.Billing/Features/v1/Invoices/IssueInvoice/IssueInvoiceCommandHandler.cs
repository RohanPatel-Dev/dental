using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Contracts.Events;
using Dental.Modules.Billing.Contracts.v1.Invoices.IssueInvoice;
using Dental.Modules.Billing.Data;
using Dental.Modules.Billing.Domain;
using Dental.Modules.Billing.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Billing.Features.v1.Invoices.IssueInvoice;

/// <summary>Presents a draft invoice to the patient.</summary>
/// <param name="context">The billing context.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="timeProvider">Clock.</param>
public sealed class IssueInvoiceCommandHandler(
    BillingDbContext context,
    IOutboxStore outbox,
    TimeProvider timeProvider) : ICommandHandler<IssueInvoiceCommand, InvoiceDto>
{
    /// <inheritdoc />
    public async ValueTask<InvoiceDto> Handle(
        IssueInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Invoice invoice = await context.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Invoice", command.InvoiceId);

        invoice.Issue(timeProvider.GetUtcNow());

        await outbox.AddAsync(
                new InvoiceIssuedIntegrationEvent(
                    invoice.Id,
                    invoice.PatientId,
                    invoice.Number,
                    invoice.Total,
                    invoice.Currency)
                {
                    TenantId = invoice.TenantId,
                    Source = nameof(Billing),
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return InvoiceService.Map(invoice);
    }
}
