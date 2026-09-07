using Dental.Framework.Eventing.Abstractions;
using Dental.Framework.Eventing.Outbox;
using Dental.Modules.Clinical.Contracts.Events;
using Dental.Modules.Clinical.Data;
using Dental.Modules.Clinical.Domain;
using Dental.Modules.Scheduling.Contracts.Events;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Clinical.Events;

/// <summary>
/// Marks the patient's planned items delivered when their appointment completes, then announces the
/// delivery so Billing can raise the charges.
/// </summary>
/// <remarks>
/// A chain of three modules that never reference each other's runtime: Scheduling says "appointment
/// completed", Clinical decides what that means clinically, and Billing turns the result into money.
/// </remarks>
/// <param name="context">The clinical context.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="logger">Logger.</param>
public sealed class AppointmentCompletedHandler(
    ClinicalDbContext context,
    IOutboxStore<ClinicalDbContext> outbox,
    ILogger<AppointmentCompletedHandler> logger)
    : IIntegrationEventHandler<AppointmentCompletedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        AppointmentCompletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        List<TreatmentPlan> plans = await context.TreatmentPlans
            .Include(p => p.Items)
            .Where(p => p.PatientId == integrationEvent.PatientId
                        && p.Status == Contracts.Dtos.TreatmentPlanStatus.Accepted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        List<DeliveredProcedurePayload> delivered = [];

        foreach (TreatmentPlan plan in plans)
        {
            foreach (TreatmentPlanItem item in plan.Items.Where(i => !i.IsDelivered))
            {
                item.IsDelivered = true;
                item.DeliveredAtAppointmentId = integrationEvent.AppointmentId;
                item.DeliveredAt = integrationEvent.CompletedAt;

                delivered.Add(new DeliveredProcedurePayload(
                    item.ProcedureId,
                    item.ProcedureCode,
                    item.Description,
                    item.ToothNumber,
                    item.Fee,
                    plan.Currency));
            }

            plan.CompleteIfFullyDelivered();
        }

        if (delivered.Count == 0)
        {
            logger.LogInformation(
                "Appointment {AppointmentId} completed with no outstanding planned items to deliver.",
                integrationEvent.AppointmentId);
            return;
        }

        await outbox.AddAsync(
                new ProceduresDeliveredIntegrationEvent(
                    integrationEvent.AppointmentId,
                    integrationEvent.PatientId,
                    integrationEvent.ProviderId,
                    delivered)
                {
                    TenantId = integrationEvent.TenantId,
                    CorrelationId = integrationEvent.CorrelationId,
                    Source = nameof(Clinical),
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Delivered {Count} planned item(s) at appointment {AppointmentId}.",
            delivered.Count,
            integrationEvent.AppointmentId);
    }
}
