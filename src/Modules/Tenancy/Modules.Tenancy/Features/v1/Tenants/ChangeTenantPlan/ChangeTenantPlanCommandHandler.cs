using Dental.Framework.Core.Exceptions;
using Dental.Framework.Shared.Caching;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Dental.Modules.Tenancy.Contracts.v1.Tenants.ChangeTenantPlan;
using Dental.Modules.Tenancy.Data;
using Dental.Modules.Tenancy.Domain;
using Dental.Modules.Tenancy.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.ChangeTenantPlan;

/// <summary>Moves a tenant onto a different plan, which changes its quota limits immediately.</summary>
/// <param name="context">The tenancy context.</param>
/// <param name="cache">Shared cache, invalidated after the write.</param>
public sealed class ChangeTenantPlanCommandHandler(TenancyDbContext context, HybridCache cache)
    : ICommandHandler<ChangeTenantPlanCommand, TenantDto>
{
    /// <inheritdoc />
    public async ValueTask<TenantDto> Handle(
        ChangeTenantPlanCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Tenant tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Identifier == command.Identifier, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Tenant", command.Identifier);

        bool planExists = await context.Plans
            .AnyAsync(p => p.Name == command.Plan, cancellationToken)
            .ConfigureAwait(false);

        if (!planExists)
        {
            throw new NotFoundException($"Plan '{command.Plan}' does not exist.");
        }

        tenant.ChangePlan(command.Plan);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(CacheKeys.Tags.Tenants, cancellationToken).ConfigureAwait(false);

        return TenantService.Map(tenant)!;
    }
}
