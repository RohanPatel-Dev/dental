using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Framework.Shared.Caching;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Dental.Modules.Tenancy.Contracts.Events;
using Dental.Modules.Tenancy.Contracts.v1.Tenants.SetTenantStatus;
using Dental.Modules.Tenancy.Data;
using Dental.Modules.Tenancy.Domain;
using Dental.Modules.Tenancy.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.SetTenantStatus;

/// <summary>Activates or deactivates a tenant.</summary>
/// <param name="context">The tenancy context.</param>
/// <param name="outbox">Outbox writer.</param>
/// <param name="cache">Shared cache, invalidated after the write.</param>
public sealed class SetTenantStatusCommandHandler(
    TenancyDbContext context,
    IOutboxStore<TenancyDbContext> outbox,
    HybridCache cache) : ICommandHandler<SetTenantStatusCommand, TenantDto>
{
    /// <inheritdoc />
    public async ValueTask<TenantDto> Handle(
        SetTenantStatusCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Tracked on purpose: this is a read-then-mutate flow, so AsNoTracking would silently
        // discard the change at SaveChanges.
        Tenant tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Identifier == command.Identifier, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Tenant", command.Identifier);

        if (command.IsActive)
        {
            tenant.Activate();
        }
        else
        {
            string reason = command.Reason ?? "No reason supplied.";
            tenant.Deactivate(reason);

            await outbox.AddAsync(
                    new TenantDeactivatedIntegrationEvent(reason)
                    {
                        TenantId = tenant.Identifier,
                        Source = nameof(Tenancy),
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(CacheKeys.Tags.Tenants, cancellationToken).ConfigureAwait(false);

        return TenantService.Map(tenant)!;
    }
}
