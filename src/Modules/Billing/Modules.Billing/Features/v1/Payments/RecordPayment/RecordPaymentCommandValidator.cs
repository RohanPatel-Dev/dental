using Dental.Modules.Billing.Contracts.v1.Payments.RecordPayment;
using FluentValidation;

namespace Dental.Modules.Billing.Features.v1.Payments.RecordPayment;

/// <summary>Validates <see cref="RecordPaymentCommand"/>.</summary>
public sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    /// <summary>Builds the rules.</summary>
    public RecordPaymentCommandValidator()
    {
        RuleFor(c => c.InvoiceId).NotEmpty();
        RuleFor(c => c.Method).IsInEnum();

        RuleFor(c => c.Amount)
            .GreaterThan(0)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true);

        // A card terminal receipt number, never a card number - the reference is stored in clear.
        RuleFor(c => c.Reference).MaximumLength(128);
    }
}
