using Dental.Framework.Shared.Pagination;
using Dental.Modules.Auditing.Contracts.v1.AuditTrails.SearchAuditTrails;
using FluentValidation;

namespace Dental.Modules.Auditing.Features.v1.AuditTrails.SearchAuditTrails;

/// <summary>Validates <see cref="SearchAuditTrailsQuery"/>.</summary>
public sealed class SearchAuditTrailsQueryValidator : AbstractValidator<SearchAuditTrailsQuery>
{
    /// <summary>Builds the rules.</summary>
    public SearchAuditTrailsQueryValidator()
    {
        RuleFor(q => q.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.MinPageNumber);
        RuleFor(q => q.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        RuleFor(q => q.EntityName).MaximumLength(256);
        RuleFor(q => q.EntityId).MaximumLength(128);
        RuleFor(q => q.Operation).MaximumLength(32);

        RuleFor(q => q.To)
            .GreaterThan(q => q.From!.Value)
            .When(q => q.From.HasValue && q.To.HasValue)
            .WithMessage("'To' must be after 'From'.");
    }
}
