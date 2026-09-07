using Dental.Framework.Shared.Pagination;
using Dental.Modules.Scheduling.Contracts.v1.Appointments.SearchAppointments;
using FluentValidation;

namespace Dental.Modules.Scheduling.Features.v1.Appointments.SearchAppointments;

/// <summary>Validates <see cref="SearchAppointmentsQuery"/>.</summary>
public sealed class SearchAppointmentsQueryValidator : AbstractValidator<SearchAppointmentsQuery>
{
    /// <summary>Builds the rules.</summary>
    public SearchAppointmentsQueryValidator()
    {
        RuleFor(q => q.PageNumber).GreaterThanOrEqualTo(PaginationDefaults.MinPageNumber);
        RuleFor(q => q.PageSize).InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        RuleFor(q => q.Status).IsInEnum().When(q => q.Status.HasValue);

        RuleFor(q => q.To)
            .GreaterThan(q => q.From!.Value)
            .When(q => q.From.HasValue && q.To.HasValue)
            .WithMessage("'To' must be after 'From'.");
    }
}
