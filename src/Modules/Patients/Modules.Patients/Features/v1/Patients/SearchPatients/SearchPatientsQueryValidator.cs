using Dental.Framework.Shared.Pagination;
using Dental.Modules.Patients.Contracts.v1.Patients.SearchPatients;
using FluentValidation;

namespace Dental.Modules.Patients.Features.v1.Patients.SearchPatients;

/// <summary>Validates <see cref="SearchPatientsQuery"/>.</summary>
public sealed class SearchPatientsQueryValidator : AbstractValidator<SearchPatientsQuery>
{
    /// <summary>Builds the rules.</summary>
    public SearchPatientsQueryValidator()
    {
        RuleFor(q => q.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.MinPageNumber);
        RuleFor(q => q.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        RuleFor(q => q.SearchTerm).MaximumLength(256);
        RuleFor(q => q.Status).IsInEnum().When(q => q.Status.HasValue);
    }
}
