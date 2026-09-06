using Dental.Framework.Persistence.Pagination;
using Dental.Framework.Shared.Pagination;
using Dental.Modules.Auditing.Contracts.Dtos;
using Dental.Modules.Auditing.Contracts.v1.AuditTrails.SearchAuditTrails;
using Dental.Modules.Auditing.Data;
using Dental.Modules.Auditing.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Auditing.Features.v1.AuditTrails.SearchAuditTrails;

/// <summary>Pages through the audit trail of the current tenant.</summary>
/// <param name="context">The auditing context.</param>
public sealed class SearchAuditTrailsQueryHandler(AuditingDbContext context)
    : IQueryHandler<SearchAuditTrailsQuery, PagedResponse<AuditTrailDto>>
{
    /// <inheritdoc />
    public async ValueTask<PagedResponse<AuditTrailDto>> Handle(
        SearchAuditTrailsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // No IgnoreQueryFilters here: the tenant filter is exactly what keeps one practice from
        // reading another's audit trail.
        IQueryable<AuditTrail> trails = context.AuditTrails.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            trails = trails.Where(a => a.EntityName == query.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            trails = trails.Where(a => a.EntityId == query.EntityId);
        }

        if (query.UserId is { } userId)
        {
            trails = trails.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(query.Operation))
        {
            trails = trails.Where(a => a.Operation == query.Operation);
        }

        if (query.From is { } from)
        {
            trails = trails.Where(a => a.OccurredOnUtc >= from);
        }

        if (query.To is { } to)
        {
            trails = trails.Where(a => a.OccurredOnUtc <= to);
        }

        trails = query.Sort == "occurredOn asc"
            ? trails.OrderBy(a => a.OccurredOnUtc)
            : trails.OrderByDescending(a => a.OccurredOnUtc);

        return await trails
            .Select(a => new AuditTrailDto(
                a.Id,
                a.EntityName,
                a.EntityId,
                a.Operation,
                a.Module,
                a.UserId,
                a.OccurredOnUtc,
                a.CorrelationId,
                a.ChangedColumns.Length == 0
                    ? new List<string>()
                    : a.ChangedColumns.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                a.OldValues,
                a.NewValues))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
