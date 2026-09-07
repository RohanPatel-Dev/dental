using Dental.Framework.Core.Exceptions;
using Dental.Framework.Eventing.Outbox;
using Dental.Framework.Shared.Caching;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Dental.Modules.Tenancy.Contracts.Events;
using Dental.Modules.Tenancy.Contracts.v1.Tenants.CreateTenant;
using Dental.Modules.Tenancy.Data;
using Dental.Modules.Tenancy.Domain;
using Dental.Modules.Tenancy.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.CreateTenant;

/// <summary>Provisions a tenant and announces it so other modules can seed their own defaults.</summary>
/// <param name="context">The tenancy context.</param>
/// <param name="outbox">Outbox writer - the only supported way to publish.</param>
/// <param name="cache">Shared cache, invalidated after the write.</param>
public sealed class CreateTenantCommandHandler(
    TenancyDbContext context,
    IOutboxStore<TenancyDbContext> outbox,
    HybridCache cache) : ICommandHandler<CreateTenantCommand, TenantDto>
{
    /// <inheritdoc />
    public async ValueTask<TenantDto> Handle(
        CreateTenantCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string identifier = command.Identifier.Trim().ToLowerInvariant();

        bool taken = await context.Tenants
            .AnyAsync(t => t.Identifier == identifier, cancellationToken)
            .ConfigureAwait(false);

        if (taken)
        {
            throw new ConflictException($"A tenant with identifier '{identifier}' already exists.");
        }

        bool planExists = await context.Plans
            .AnyAsync(p => p.Name == command.Plan, cancellationToken)
            .ConfigureAwait(false);

        if (!planExists)
        {
            throw new NotFoundException($"Plan '{command.Plan}' does not exist.");
        }

        Tenant tenant = new()
        {
            Identifier = identifier,
            Name = command.Name.Trim(),
            AdminEmail = command.AdminEmail.Trim(),
            PlanName = command.Plan,
            TimeZone = command.TimeZone,
            ValidUntil = command.ValidUntil,
            IsActive = true,
            TenantId = identifier,
        };

        context.Tenants.Add(tenant);

        // Written in the SAME transaction as the tenant row: a crash between the two is impossible.
        await outbox.AddAsync(
                new TenantProvisionedIntegrationEvent(identifier, tenant.Name, tenant.AdminEmail)
                {
                    TenantId = identifier,
                    Source = nameof(Tenancy),
                },
                cancellationToken)
            .ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cache.RemoveByTagAsync(CacheKeys.Tags.Tenants, cancellationToken).ConfigureAwait(false);

        return TenantService.Map(tenant)!;
    }
}
