using Dental.Modules.Clinical.Contracts.v1.Procedures.CreateProcedure;
using FluentValidation;

namespace Dental.Modules.Clinical.Features.v1.Procedures.CreateProcedure;

/// <summary>Validates <see cref="CreateProcedureCommand"/>.</summary>
public sealed class CreateProcedureCommandValidator : AbstractValidator<CreateProcedureCommand>
{
    /// <summary>Builds the rules.</summary>
    public CreateProcedureCommandValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(512);
        RuleFor(c => c.Category).IsInEnum();
        RuleFor(c => c.DefaultFee).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
        RuleFor(c => c.Currency).NotEmpty().Length(3);
        RuleFor(c => c.DefaultDurationMinutes).InclusiveBetween(5, 8 * 60);
    }
}
