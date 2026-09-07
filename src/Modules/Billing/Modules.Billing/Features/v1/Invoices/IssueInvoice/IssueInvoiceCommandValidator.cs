using Dental.Modules.Billing.Contracts.v1.Invoices.IssueInvoice;
using FluentValidation;

namespace Dental.Modules.Billing.Features.v1.Invoices.IssueInvoice;

/// <summary>Validates <see cref="IssueInvoiceCommand"/>.</summary>
public sealed class IssueInvoiceCommandValidator : AbstractValidator<IssueInvoiceCommand>
{
    /// <summary>Builds the rules.</summary>
    public IssueInvoiceCommandValidator() => RuleFor(c => c.InvoiceId).NotEmpty();
}
