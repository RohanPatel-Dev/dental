using Dental.Framework.Eventing.Abstractions;
using Dental.Modules.Notifications.Data;
using Dental.Modules.Notifications.Services;
using Dental.Modules.Patients.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Notifications.Events;

/// <summary>Seeds the local patient projection when a patient is registered.</summary>
/// <param name="context">The notifications context.</param>
/// <param name="projection">The local projection.</param>
public sealed class PatientRegisteredHandler(
    NotificationsDbContext context,
    PatientContactProjection projection)
    : IIntegrationEventHandler<PatientRegisteredIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        PatientRegisteredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        await projection.UpsertAsync(
                integrationEvent.PatientId,
                integrationEvent.TenantId ?? string.Empty,
                contact =>
                {
                    contact.FullName = integrationEvent.FullName;
                    contact.Email = integrationEvent.Email;
                    contact.PhoneNumber = integrationEvent.PhoneNumber;
                    contact.HasReminderConsent = integrationEvent.HasReminderConsent;
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Keeps the local patient projection current when contact details change.</summary>
/// <param name="context">The notifications context.</param>
/// <param name="projection">The local projection.</param>
/// <param name="logger">Logger.</param>
public sealed class PatientContactChangedHandler(
    NotificationsDbContext context,
    PatientContactProjection projection,
    ILogger<PatientContactChangedHandler> logger)
    : IIntegrationEventHandler<PatientContactChangedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        PatientContactChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        await projection.UpsertAsync(
                integrationEvent.PatientId,
                integrationEvent.TenantId ?? string.Empty,
                contact =>
                {
                    contact.FullName = integrationEvent.FullName;
                    contact.Email = integrationEvent.Email;
                    contact.PhoneNumber = integrationEvent.PhoneNumber;
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug(
            "Updated the local contact projection for patient {PatientId}.",
            integrationEvent.PatientId);
    }
}
