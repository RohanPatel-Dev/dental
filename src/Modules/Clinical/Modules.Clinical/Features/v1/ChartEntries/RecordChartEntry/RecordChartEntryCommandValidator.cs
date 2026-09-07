using Dental.Modules.Clinical.Contracts.v1.ChartEntries.RecordChartEntry;
using FluentValidation;

namespace Dental.Modules.Clinical.Features.v1.ChartEntries.RecordChartEntry;

/// <summary>Validates <see cref="RecordChartEntryCommand"/>.</summary>
public sealed class RecordChartEntryCommandValidator : AbstractValidator<RecordChartEntryCommand>
{
    /// <summary>Builds the rules.</summary>
    public RecordChartEntryCommandValidator()
    {
        RuleFor(c => c.PatientId).NotEmpty();
        RuleFor(c => c.ProviderId).NotEmpty();
        RuleFor(c => c.Condition).IsInEnum();
        RuleFor(c => c.Notes).MaximumLength(4000);

        RuleFor(c => c.ToothNumber)
            .Must(tooth => tooth / 10 is >= 1 and <= 8 && tooth % 10 is >= 1 and <= 8)
            .WithMessage("ToothNumber must be a valid FDI tooth number.");

        // Mesial, Occlusal, Distal, Buccal, Lingual, Incisal - the standard surface letters.
        RuleFor(c => c.Surfaces)
            .Matches("^[MODBLI]{1,5}$")
            .When(c => !string.IsNullOrEmpty(c.Surfaces))
            .WithMessage("Surfaces must be a combination of M, O, D, B, L and I.");
    }
}
