using Dental.Framework.Shared.Pagination;
using Dental.Modules.Identity.Contracts.v1.Users.SearchUsers;
using FluentValidation;

namespace Dental.Modules.Identity.Features.v1.Users.SearchUsers;

/// <summary>Validates <see cref="SearchUsersQuery"/>.</summary>
public sealed class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    /// <summary>Builds the rules.</summary>
    public SearchUsersQueryValidator()
    {
        RuleFor(q => q.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.MinPageNumber);
        RuleFor(q => q.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        RuleFor(q => q.SearchTerm).MaximumLength(256);
        RuleFor(q => q.Role).MaximumLength(128);
    }
}
