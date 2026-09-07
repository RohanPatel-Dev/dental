using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Data;
using Dental.Modules.Clinical.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.AcceptTreatmentPlan;

/// <summary>
/// Declines a patient's other outstanding proposals once they accept one.
/// </summary>
/// <remarks>
/// This is what a DOMAIN event is for, as opposed to an integration event: the work is in-process,
/// inside this module, and enforces an invariant Clinical owns - a patient has at most one live
/// course of treatment. Nothing outside Clinical needs to know it happened, so it never reaches the
/// outbox.
/// Domain events are dispatched after the unit of work is saved, so the handler observes the
/// accepted plan as persisted and issues its own update.
/// </remarks>
/// <param name="context">The clinical context.</param>
/// <param name="logger">Logger.</param>
public sealed class SupersedeProposedPlansHandler(
    ClinicalDbContext context,
    ILogger<SupersedeProposedPlansHandler> logger)
    : INotificationHandler<TreatmentPlanAcceptedDomainEvent>
{
    /// <inheritdoc />
    public async ValueTask Handle(
        TreatmentPlanAcceptedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        int superseded = await context.TreatmentPlans
            .Where(p => p.PatientId == notification.PatientId
                        && p.Id != notification.TreatmentPlanId
                        && (p.Status == TreatmentPlanStatus.Draft
                            || p.Status == TreatmentPlanStatus.Proposed))
            .ExecuteUpdateAsync(
                update => update.SetProperty(p => p.Status, TreatmentPlanStatus.Declined),
                cancellationToken)
            .ConfigureAwait(false);

        if (superseded > 0)
        {
            logger.LogInformation(
                "Superseded {Count} outstanding proposal(s) for patient {PatientId}.",
                superseded,
                notification.PatientId);
        }
    }
}
