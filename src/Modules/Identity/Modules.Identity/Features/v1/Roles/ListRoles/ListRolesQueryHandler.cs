using Dental.Framework.Shared.Identity;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Roles.ListRoles;
using Dental.Modules.Identity.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Identity.Features.v1.Roles.ListRoles;

/// <summary>Lists the roles of the current tenant with their permission sets.</summary>
/// <param name="context">The identity context.</param>
public sealed class ListRolesQueryHandler(IdentityModuleDbContext context)
    : IQueryHandler<ListRolesQuery, IReadOnlyList<RoleDto>>
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<RoleDto>> Handle(
        ListRolesQuery query,
        CancellationToken cancellationToken)
    {
        var roles = await context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.Name, r.Description, r.IsBuiltIn })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<Guid, List<string>> permissionsByRole = await context.RoleClaims
            .AsNoTracking()
            .Where(rc => rc.ClaimType == DentalClaims.Permission)
            .GroupBy(rc => rc.RoleId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(rc => rc.ClaimValue ?? string.Empty).ToList(),
                cancellationToken)
            .ConfigureAwait(false);

        return
        [
            .. roles.Select(r => new RoleDto(
                r.Id,
                r.Name ?? string.Empty,
                r.Description,
                r.IsBuiltIn,
                permissionsByRole.TryGetValue(r.Id, out List<string>? permissions) ? permissions : [])),
        ];
    }
}
