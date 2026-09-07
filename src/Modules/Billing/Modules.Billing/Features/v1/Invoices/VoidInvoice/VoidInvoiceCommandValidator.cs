using Dental.Modules.Billing.Contracts.v1.Invoices.VoidInvoice;
using FluentValidation;

namespace Dental.Modules.Billing.Features.v1.Invoices.VoidInvoice;

/// <summary>Validates <see cref="VoidInvoiceCommand"/>.</summary>
public sealed class VoidInvoiceCommandValidator : AbstractValidator<VoidInvoiceCommand>
{
    /// <summary>Builds the rules.</summary>
    public VoidInvoiceCommandValidator()
    {
        RuleFor(c => c.InvoiceId).NotEmpty();

        // Voiding an invoice is a financial correction; the reason belongs in the audit trail.
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(512);
    }
}
