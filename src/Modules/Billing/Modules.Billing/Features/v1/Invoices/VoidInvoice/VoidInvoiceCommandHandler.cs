using Dental.Framework.Core.Exceptions;
using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Contracts.v1.Invoices.VoidInvoice;
using Dental.Modules.Billing.Data;
using Dental.Modules.Billing.Domain;
using Dental.Modules.Billing.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Billing.Features.v1.Invoices.VoidInvoice;

/// <summary>Cancels an invoice without payment.</summary>
/// <param name="context">The billing context.</param>
public sealed class VoidInvoiceCommandHandler(BillingDbContext context)
    : ICommandHandler<VoidInvoiceCommand, InvoiceDto>
{
    /// <inheritdoc />
    public async ValueTask<InvoiceDto> Handle(
        VoidInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Invoice invoice = await context.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Invoice", command.InvoiceId);

        invoice.Void(command.Reason);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return InvoiceService.Map(invoice);
    }
}
