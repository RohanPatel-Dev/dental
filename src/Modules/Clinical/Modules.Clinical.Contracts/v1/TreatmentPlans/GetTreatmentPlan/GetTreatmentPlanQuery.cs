using Dental.Modules.Clinical.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.GetTreatmentPlan;

/// <summary>Reads one treatment plan with its items.</summary>
/// <param name="TreatmentPlanId">Plan identifier.</param>
public sealed record GetTreatmentPlanQuery(Guid TreatmentPlanId) : IQuery<TreatmentPlanDto>;
