using Dental.Framework.Eventing.Abstractions;
using Dental.Modules.Billing.Contracts.Events;
using Dental.Modules.Notifications.Contracts.Dtos;
using Dental.Modules.Notifications.Data;
using Dental.Modules.Notifications.Services;

namespace Dental.Modules.Notifications.Events;

/// <summary>Queues a notice when an invoice is presented to a patient.</summary>
/// <param name="context">The notifications context.</param>
/// <param name="queue">Queues messages, applying the consent checks.</param>
public sealed class InvoiceIssuedHandler(NotificationsDbContext context, NotificationQueue queue)
    : IIntegrationEventHandler<InvoiceIssuedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        InvoiceIssuedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        await queue.EnqueueAsync(
                integrationEvent.PatientId,
                NotificationKind.InvoiceIssued,
                integrationEvent.InvoiceId,
                integrationEvent.OccurredOnUtc,
                (patient, composer) => composer.ComposeInvoice(
                    patient,
                    integrationEvent.Number,
                    integrationEvent.Total,
                    integrationEvent.Currency),
                integrationEvent.TenantId ?? string.Empty,
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
