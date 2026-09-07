using Dental.Framework.Shared.Pagination;
using Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.SearchTreatmentPlans;
using FluentValidation;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.SearchTreatmentPlans;

/// <summary>Validates <see cref="SearchTreatmentPlansQuery"/>.</summary>
public sealed class SearchTreatmentPlansQueryValidator : AbstractValidator<SearchTreatmentPlansQuery>
{
    /// <summary>Builds the rules.</summary>
    public SearchTreatmentPlansQueryValidator()
    {
        RuleFor(q => q.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.MinPageNumber);

        // Plans eager-load their items, so a large page multiplies rows; cap it tighter than usual.
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);

        RuleFor(q => q.Status).IsInEnum().When(q => q.Status.HasValue);
    }
}
