using Dental.Framework.Shared.Pagination;
using Dental.Modules.Auditing.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Auditing.Contracts.v1.AuditTrails.SearchAuditTrails;

/// <summary>Pages through the audit trail of the current tenant.</summary>
/// <param name="EntityName">Filters by entity type.</param>
/// <param name="EntityId">Filters by the identifier of one row.</param>
/// <param name="UserId">Filters by the user that made the change.</param>
/// <param name="Operation">Filters by operation name.</param>
/// <param name="From">Earliest change to include.</param>
/// <param name="To">Latest change to include.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchAuditTrailsQuery(
    string? EntityName,
    string? EntityId,
    Guid? UserId,
    string? Operation,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int PageNumber,
    int PageSize,
    string? Sort) : IQuery<PagedResponse<AuditTrailDto>>, IPagedQuery;
