using Dental.Modules.Billing.Contracts.v1.Invoices.GetInvoice;
using FluentValidation;

namespace Dental.Modules.Billing.Features.v1.Invoices.GetInvoice;

/// <summary>Validates <see cref="GetInvoiceQuery"/>.</summary>
public sealed class GetInvoiceQueryValidator : AbstractValidator<GetInvoiceQuery>
{
    /// <summary>Builds the rules.</summary>
    public GetInvoiceQueryValidator() => RuleFor(q => q.InvoiceId).NotEmpty();
}
