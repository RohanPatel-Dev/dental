using Dental.Framework.Eventing.Abstractions;
using Dental.Modules.Identity.Data;
using Dental.Modules.Tenancy.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Identity.Events;

/// <summary>
/// Seeds the built-in roles and the first administrator account whenever a practice is provisioned.
/// </summary>
/// <remarks>
/// This is the whole point of cross-module eventing: Tenancy knows nothing about users, and Identity
/// knows nothing about how a tenant is created. They meet at a contract record.
/// The handler runs in a fresh scope with the tenant context already restored by the dispatcher,
/// and the inbox guarantees it runs at most once per delivery.
/// </remarks>
/// <param name="seeder">Seeds roles and the administrator.</param>
/// <param name="logger">Logger.</param>
public sealed class TenantProvisionedHandler(
    IdentitySeeder seeder,
    ILogger<TenantProvisionedHandler> logger)
    : IIntegrationEventHandler<TenantProvisionedIntegrationEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(
        TenantProvisionedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        string tenantId = integrationEvent.TenantId
            ?? throw new InvalidOperationException(
                "TenantProvisionedIntegrationEvent arrived without a tenant identifier.");

        await seeder.SeedRolesAsync(tenantId, cancellationToken).ConfigureAwait(false);

        await seeder.SeedAdministratorAsync(
                tenantId,
                integrationEvent.AdminEmail,
                integrationEvent.Name,
                cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Seeded identity for newly provisioned tenant {TenantId}.",
            tenantId);
    }
}
