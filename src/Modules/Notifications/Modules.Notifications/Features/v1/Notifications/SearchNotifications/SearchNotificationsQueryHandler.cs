using Dental.Framework.Persistence.Pagination;
using Dental.Framework.Shared.Pagination;
using Dental.Modules.Notifications.Contracts.Dtos;
using Dental.Modules.Notifications.Contracts.v1.Notifications.SearchNotifications;
using Dental.Modules.Notifications.Data;
using Dental.Modules.Notifications.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Notifications.Features.v1.Notifications.SearchNotifications;

/// <summary>Pages through the outbound notification log.</summary>
/// <param name="context">The notifications context.</param>
public sealed class SearchNotificationsQueryHandler(NotificationsDbContext context)
    : IQueryHandler<SearchNotificationsQuery, PagedResponse<NotificationDto>>
{
    /// <inheritdoc />
    public async ValueTask<PagedResponse<NotificationDto>> Handle(
        SearchNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Notification> notifications = context.Notifications.AsNoTracking();

        if (query.PatientId is { } patientId)
        {
            notifications = notifications.Where(n => n.PatientId == patientId);
        }

        if (query.Kind is { } kind)
        {
            notifications = notifications.Where(n => n.Kind == kind);
        }

        if (query.Status is { } status)
        {
            notifications = notifications.Where(n => n.Status == status);
        }

        notifications = query.Sort == "scheduledFor asc"
            ? notifications.OrderBy(n => n.ScheduledFor)
            : notifications.OrderByDescending(n => n.ScheduledFor);

        // The rendered body is deliberately NOT projected: it is a copy of the patient's details,
        // and an admin list view has no reason to carry it.
        return await notifications
            .Select(n => new NotificationDto(
                n.Id,
                n.PatientId,
                n.Kind,
                n.Status,
                n.ScheduledFor,
                n.SentAt,
                n.Subject,
                n.Error))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
