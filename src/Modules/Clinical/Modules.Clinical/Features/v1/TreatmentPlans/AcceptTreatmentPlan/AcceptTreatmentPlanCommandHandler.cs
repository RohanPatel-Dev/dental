using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.Events;
using Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.AcceptTreatmentPlan;
using Dental.Modules.Clinical.Data;
using Dental.Modules.Clinical.Domain;
using Dental.Modules.Clinical.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.AcceptTreatmentPlan;

/// <summary>Records the patient's acceptance of a plan.</summary>
/// <param name="context">The clinical context.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="timeProvider">Clock.</param>
public sealed class AcceptTreatmentPlanCommandHandler(
    ClinicalDbContext context,
    IOutboxStore<ClinicalDbContext> outbox,
    TimeProvider timeProvider) : ICommandHandler<AcceptTreatmentPlanCommand, TreatmentPlanDto>
{
    /// <inheritdoc />
    public async ValueTask<TreatmentPlanDto> Handle(
        AcceptTreatmentPlanCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        TreatmentPlan plan = await context.TreatmentPlans
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == command.TreatmentPlanId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("TreatmentPlan", command.TreatmentPlanId);

        plan.Accept(timeProvider.GetUtcNow());

        await outbox.AddAsync(
                new TreatmentPlanAcceptedIntegrationEvent(
                    plan.Id,
                    plan.PatientId,
                    plan.TotalFee,
                    plan.Currency)
                {
                    TenantId = plan.TenantId,
                    Source = nameof(Clinical),
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return TreatmentPlanService.Map(plan);
    }
}
