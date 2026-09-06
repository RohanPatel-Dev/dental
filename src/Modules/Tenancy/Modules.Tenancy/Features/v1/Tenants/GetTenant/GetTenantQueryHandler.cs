using Dental.Framework.Core.Exceptions;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Dental.Modules.Tenancy.Contracts.Services;
using Dental.Modules.Tenancy.Contracts.v1.Tenants.GetTenant;
using Mediator;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.GetTenant;

/// <summary>Reads one tenant.</summary>
/// <param name="tenantService">Tenant lookups, cached.</param>
public sealed class GetTenantQueryHandler(ITenantService tenantService)
    : IQueryHandler<GetTenantQuery, TenantDto>
{
    /// <inheritdoc />
    public async ValueTask<TenantDto> Handle(GetTenantQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        TenantDto? tenant = await tenantService.GetAsync(query.Identifier, cancellationToken)
            .ConfigureAwait(false);

        return tenant ?? throw NotFoundException.For("Tenant", query.Identifier);
    }
}
