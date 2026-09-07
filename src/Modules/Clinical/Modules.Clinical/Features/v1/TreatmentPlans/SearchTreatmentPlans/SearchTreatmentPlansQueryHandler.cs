using Dental.Framework.Persistence.Pagination;
using Dental.Framework.Shared.Pagination;
using Dental.Modules.Clinical.Contracts.Dtos;
using Dental.Modules.Clinical.Contracts.v1.TreatmentPlans.SearchTreatmentPlans;
using Dental.Modules.Clinical.Data;
using Dental.Modules.Clinical.Domain;
using Dental.Modules.Clinical.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Clinical.Features.v1.TreatmentPlans.SearchTreatmentPlans;

/// <summary>Pages through treatment plans.</summary>
/// <param name="context">The clinical context.</param>
public sealed class SearchTreatmentPlansQueryHandler(ClinicalDbContext context)
    : IQueryHandler<SearchTreatmentPlansQuery, PagedResponse<TreatmentPlanDto>>
{
    /// <inheritdoc />
    public async ValueTask<PagedResponse<TreatmentPlanDto>> Handle(
        SearchTreatmentPlansQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<TreatmentPlan> plans = context.TreatmentPlans
            .AsNoTracking()
            .Include(p => p.Items);

        if (query.PatientId is { } patientId)
        {
            plans = plans.Where(p => p.PatientId == patientId);
        }

        if (query.ProviderId is { } providerId)
        {
            plans = plans.Where(p => p.ProviderId == providerId);
        }

        if (query.Status is { } status)
        {
            plans = plans.Where(p => p.Status == status);
        }

        plans = query.Sort == "createdAt asc"
            ? plans.OrderBy(p => p.CreatedAt)
            : plans.OrderByDescending(p => p.CreatedAt);

        PagedResponse<TreatmentPlan> page = await plans
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<TreatmentPlanDto>(
            [.. page.Items.Select(TreatmentPlanService.Map)],
            page.PageNumber,
            page.PageSize,
            page.TotalCount);
    }
}
