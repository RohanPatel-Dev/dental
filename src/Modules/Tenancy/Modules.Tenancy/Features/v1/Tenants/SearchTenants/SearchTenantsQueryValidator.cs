using Dental.Framework.Shared.Pagination;
using Dental.Modules.Tenancy.Contracts.v1.Tenants.SearchTenants;
using FluentValidation;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.SearchTenants;

/// <summary>Validates <see cref="SearchTenantsQuery"/>. Every paginated query needs one.</summary>
public sealed class SearchTenantsQueryValidator : AbstractValidator<SearchTenantsQuery>
{
    /// <summary>Builds the rules.</summary>
    public SearchTenantsQueryValidator()
    {
        RuleFor(q => q.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.MinPageNumber);
        RuleFor(q => q.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        RuleFor(q => q.SearchTerm).MaximumLength(256);
        RuleFor(q => q.Plan).MaximumLength(64);
    }
}
