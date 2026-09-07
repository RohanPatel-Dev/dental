using Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.CreateTreatmentPlan;
using FluentValidation;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.CreateTreatmentPlan;

/// <summary>Validates <see cref="CreateTreatmentPlanCommand"/>.</summary>
public sealed class CreateTreatmentPlanCommandValidator : AbstractValidator<CreateTreatmentPlanCommand>
{
    /// <summary>Builds the rules.</summary>
    public CreateTreatmentPlanCommandValidator()
    {
        RuleFor(c => c.PatientId).NotEmpty();
        RuleFor(c => c.ProviderId).NotEmpty();
        RuleFor(c => c.Notes).MaximumLength(4000);

        RuleFor(c => c.Items).NotEmpty().WithMessage("A treatment plan needs at least one item.");

        RuleForEach(c => c.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProcedureId).NotEmpty();
            item.RuleFor(i => i.Surfaces).MaximumLength(16);

            item.RuleFor(i => i.Fee)
                .GreaterThanOrEqualTo(0)
                .When(i => i.Fee.HasValue);

            // FDI notation: quadrant 1-4 for adults, 5-8 for deciduous; position 1-8.
            item.RuleFor(i => i.ToothNumber)
                .Must(BeAValidFdiToothNumber)
                .When(i => i.ToothNumber.HasValue)
                .WithMessage("ToothNumber must be a valid FDI tooth number.");
        });
    }

    private static bool BeAValidFdiToothNumber(int? toothNumber)
    {
        if (toothNumber is not { } tooth)
        {
            return true;
        }

        int quadrant = tooth / 10;
        int position = tooth % 10;

        return quadrant is >= 1 and <= 8 && position is >= 1 and <= 8;
    }
}
