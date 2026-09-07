using Dental.Framework.Core.Exceptions;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.Services;
using Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.GetTreatmentPlan;
using Mediator;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.GetTreatmentPlan;

/// <summary>Reads one treatment plan.</summary>
/// <param name="treatmentPlans">Treatment plan lookups.</param>
public sealed class GetTreatmentPlanQueryHandler(ITreatmentPlanService treatmentPlans)
    : IQueryHandler<GetTreatmentPlanQuery, TreatmentPlanDto>
{
    /// <inheritdoc />
    public async ValueTask<TreatmentPlanDto> Handle(
        GetTreatmentPlanQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await treatmentPlans.GetAsync(query.TreatmentPlanId, cancellationToken)
                   .ConfigureAwait(false)
               ?? throw NotFoundException.For("TreatmentPlan", query.TreatmentPlanId);
    }
}
