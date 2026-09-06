using Dental.Framework.Persistence.Pagination;
using Dental.Framework.Shared.Pagination;
using Dental.Modules.Tenancy.Contracts.Dtos;
using Dental.Modules.Tenancy.Contracts.v1.Tenants.SearchTenants;
using Dental.Modules.Tenancy.Data;
using Dental.Modules.Tenancy.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Tenancy.Features.v1.Tenants.SearchTenants;

/// <summary>Pages through the tenant catalog.</summary>
/// <param name="context">The tenancy context.</param>
public sealed class SearchTenantsQueryHandler(TenancyDbContext context)
    : IQueryHandler<SearchTenantsQuery, PagedResponse<TenantDto>>
{
    /// <inheritdoc />
    public async ValueTask<PagedResponse<TenantDto>> Handle(
        SearchTenantsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Tenant> tenants = context.Tenants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = $"%{query.SearchTerm.Trim()}%";
            tenants = tenants.Where(t =>
                EF.Functions.ILike(t.Identifier, term) || EF.Functions.ILike(t.Name, term));
        }

        if (query.IsActive is { } isActive)
        {
            tenants = tenants.Where(t => t.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Plan))
        {
            tenants = tenants.Where(t => t.PlanName == query.Plan);
        }

        tenants = query.Sort switch
        {
            "name desc" => tenants.OrderByDescending(t => t.Name),
            "createdAt asc" => tenants.OrderBy(t => t.CreatedAt),
            "createdAt desc" => tenants.OrderByDescending(t => t.CreatedAt),
            _ => tenants.OrderBy(t => t.Name),
        };

        return await tenants
            .Select(t => new TenantDto(
                t.Identifier,
                t.Identifier,
                t.Name,
                t.PlanName,
                t.IsActive,
                t.ValidUntil,
                t.TimeZone,
                t.AdminEmail,
                t.CreatedAt))
            .ToPagedResponseAsync(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
