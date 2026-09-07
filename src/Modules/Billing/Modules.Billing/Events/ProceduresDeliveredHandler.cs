using Dental.Framework.Eventing.Abstractions;
using Dental.Modules.Billing.Contracts.Dtos;
using Dental.Modules.Billing.Data;
using Dental.Modules.Billing.Domain;
using Dental.Modules.Billing.Services;
using Dental.Modules.Clinical.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Billing.Events;

/// <summary>
/// Raises the charges for procedures the Clinical module reports as delivered, adding them to the
/// patient's open draft invoice or starting a new one.
/// </summary>
/// <remarks>
/// Runs in a fresh scope with the tenant restored by the dispatcher, and the inbox makes it
/// idempotent - a redelivered event will not double-charge the patient.
/// </remarks>
/// <param name="context">The billing context.</param>
/// <param name="invoiceNumbers">Allocates a number for a new draft.</param>
/// <param name="logger">Logger.</param>
public sealed class ProceduresDeliveredHandler(
    BillingDbContext context,
    InvoiceNumberGenerator invoiceNumbers,
    ILogger<ProceduresDeliveredHandler> logger)
    : IIntegrationEventHandler<ProceduresDeliveredIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        ProceduresDeliveredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (integrationEvent.Procedures.Count == 0)
        {
            return;
        }

        string tenantId = integrationEvent.TenantId ?? string.Empty;
        string currency = integrationEvent.Procedures[0].Currency;

        Invoice invoice = await context.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(
                i => i.PatientId == integrationEvent.PatientId
                     && i.Status == InvoiceStatus.Draft
                     && i.Currency == currency,
                cancellationToken)
            .ConfigureAwait(false)
            ?? await StartDraftAsync(integrationEvent.PatientId, currency, tenantId, cancellationToken)
                .ConfigureAwait(false);

        foreach (DeliveredProcedurePayload procedure in integrationEvent.Procedures)
        {
            invoice.AddLine(new InvoiceLine
            {
                InvoiceId = invoice.Id,
                ProcedureCode = procedure.ProcedureCode,
                Description = procedure.Description,
                ToothNumber = procedure.ToothNumber,
                Amount = procedure.Fee,
                AppointmentId = integrationEvent.AppointmentId,
                TenantId = tenantId,
            });
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Charged {Count} procedure(s) to invoice {InvoiceNumber} for patient {PatientId}.",
            integrationEvent.Procedures.Count,
            invoice.Number,
            integrationEvent.PatientId);
    }

    private async Task<Invoice> StartDraftAsync(
        Guid patientId,
        string currency,
        string tenantId,
        CancellationToken cancellationToken)
    {
        Invoice invoice = new()
        {
            Number = await invoiceNumbers.NextAsync(cancellationToken).ConfigureAwait(false),
            PatientId = patientId,
            Status = InvoiceStatus.Draft,
            Currency = currency,
            TenantId = tenantId,
        };

        context.Invoices.Add(invoice);
        return invoice;
    }
}
