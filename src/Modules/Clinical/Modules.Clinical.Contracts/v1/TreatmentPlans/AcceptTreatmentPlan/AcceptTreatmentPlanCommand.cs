using Dental.Modules.Clinical.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.AcceptTreatmentPlan;

/// <summary>Records that the patient accepted a treatment plan.</summary>
/// <param name="TreatmentPlanId">Plan identifier.</param>
public sealed record AcceptTreatmentPlanCommand(Guid TreatmentPlanId) : ICommand<TreatmentPlanDto>;
