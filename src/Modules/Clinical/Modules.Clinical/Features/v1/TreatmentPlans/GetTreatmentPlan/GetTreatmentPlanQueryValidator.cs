using Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.GetTreatmentPlan;
using FluentValidation;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.GetTreatmentPlan;

/// <summary>Validates <see cref="GetTreatmentPlanQuery"/>.</summary>
public sealed class GetTreatmentPlanQueryValidator : AbstractValidator<GetTreatmentPlanQuery>
{
    /// <summary>Builds the rules.</summary>
    public GetTreatmentPlanQueryValidator() => RuleFor(q => q.TreatmentPlanId).NotEmpty();
}
