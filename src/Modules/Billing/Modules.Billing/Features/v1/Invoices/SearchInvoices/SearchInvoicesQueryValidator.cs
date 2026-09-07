using Dental.Framework.Shared.Pagination;
using Dental.Modules.Billing.Contracts.v1.Invoices.SearchInvoices;
using FluentValidation;

namespace Dental.Modules.Billing.Features.v1.Invoices.SearchInvoices;

/// <summary>Validates <see cref="SearchInvoicesQuery"/>.</summary>
public sealed class SearchInvoicesQueryValidator : AbstractValidator<SearchInvoicesQuery>
{
    /// <summary>Builds the rules.</summary>
    public SearchInvoicesQueryValidator()
    {
        RuleFor(q => q.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.MinPageNumber);

        // Invoices eager-load lines and payments, so a large page multiplies rows badly.
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);

        RuleFor(q => q.Status).IsInEnum().When(q => q.Status.HasValue);

        RuleFor(q => q.To)
            .GreaterThan(q => q.From!.Value)
            .When(q => q.From.HasValue && q.To.HasValue)
            .WithMessage("'To' must be after 'From'.");
    }
}
