using Dental.Framework.Shared.Pagination;
using Dental.Modules.Notifications.Contracts.v1.Notifications.SearchNotifications;
using FluentValidation;

namespace Dental.Modules.Notifications.Features.v1.Notifications.SearchNotifications;

/// <summary>Validates <see cref="SearchNotificationsQuery"/>.</summary>
public sealed class SearchNotificationsQueryValidator : AbstractValidator<SearchNotificationsQuery>
{
    /// <summary>Builds the rules.</summary>
    public SearchNotificationsQueryValidator()
    {
        RuleFor(q => q.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.MinPageNumber);
        RuleFor(q => q.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        RuleFor(q => q.Kind).IsInEnum().When(q => q.Kind.HasValue);
        RuleFor(q => q.Status).IsInEnum().When(q => q.Status.HasValue);
    }
}
