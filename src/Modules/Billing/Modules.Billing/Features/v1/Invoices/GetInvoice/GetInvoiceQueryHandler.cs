using Dental.Framework.Core.Exceptions;
using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Contracts.Services;
using Dental.Modules.Billing.Contracts.v1.Invoices.GetInvoice;
using Mediator;

namespace Dental.Modules.Billing.Features.v1.Invoices.GetInvoice;

/// <summary>Reads one invoice.</summary>
/// <param name="invoices">Invoice lookups.</param>
public sealed class GetInvoiceQueryHandler(IInvoiceService invoices)
    : IQueryHandler<GetInvoiceQuery, InvoiceDto>
{
    /// <inheritdoc />
    public async ValueTask<InvoiceDto> Handle(GetInvoiceQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await invoices.GetAsync(query.InvoiceId, cancellationToken).ConfigureAwait(false)
            ?? throw NotFoundException.For("Invoice", query.InvoiceId);
    }
}
