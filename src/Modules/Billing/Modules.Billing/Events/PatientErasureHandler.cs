using Dental.Framework.Eventing.Abstractions;
using Dental.Modules.Billing.Data;
using Dental.Modules.Billing.Domain;
using Dental.Modules.Patients.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Billing.Events;

/// <summary>
/// Strips the free-text payment references when a patient's record is erased.
/// </summary>
/// <remarks>
/// The invoices and their amounts stay: financial records carry a statutory retention period that
/// outranks an erasure request, and the rows hold no identifiers once the references are cleared.
/// </remarks>
/// <param name="context">The billing context.</param>
/// <param name="logger">Logger.</param>
public sealed class PatientErasureHandler(
    BillingDbContext context,
    ILogger<PatientErasureHandler> logger)
    : IIntegrationEventHandler<PatientErasureRequestedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        PatientErasureRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        List<Guid> invoiceIds = await context.Invoices
            .Where(i => i.PatientId == integrationEvent.PatientId)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (invoiceIds.Count == 0)
        {
            return;
        }

        int count = await context.Payments
            .Where(p => invoiceIds.Contains(p.InvoiceId) && p.Reference != null)
            .ExecuteUpdateAsync(
                update => update.SetProperty(p => p.Reference, (string?)null),
                cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Cleared {Count} payment reference(s) across {InvoiceCount} invoice(s) for patient {PatientId}.",
            count,
            invoiceIds.Count,
            integrationEvent.PatientId);
    }
}
