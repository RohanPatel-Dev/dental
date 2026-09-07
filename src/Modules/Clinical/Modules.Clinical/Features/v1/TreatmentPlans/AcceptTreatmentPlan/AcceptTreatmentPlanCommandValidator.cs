using Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.AcceptTreatmentPlan;
using FluentValidation;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.AcceptTreatmentPlan;

/// <summary>Validates <see cref="AcceptTreatmentPlanCommand"/>.</summary>
public sealed class AcceptTreatmentPlanCommandValidator : AbstractValidator<AcceptTreatmentPlanCommand>
{
    /// <summary>Builds the rules.</summary>
    public AcceptTreatmentPlanCommandValidator() => RuleFor(c => c.TreatmentPlanId).NotEmpty();
}
