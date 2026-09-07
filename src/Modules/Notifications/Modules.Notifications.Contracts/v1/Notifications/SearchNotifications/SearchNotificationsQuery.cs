using Dental.Framework.Shared.Pagination;
using Dental.Modules.Notifications.Contracts.Dtos;
using Mediator;

namespace Dental.Modules.Notifications.Contracts.v1.Notifications.SearchNotifications;

/// <summary>Pages through the outbound notification log.</summary>
/// <param name="PatientId">Filters to one patient.</param>
/// <param name="Kind">Filters by what the notification is about.</param>
/// <param name="Status">Filters by lifecycle status.</param>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Sort">Sort expression.</param>
public sealed record SearchNotificationsQuery(
    Guid? PatientId,
    NotificationKind? Kind,
    NotificationStatus? Status,
    int PageNumber,
    int PageSize,
    string? Sort) : IQuery<PagedResponse<NotificationDto>>, IPagedQuery;
