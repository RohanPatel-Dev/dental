using Dental.Framework.Core.Exceptions;
using Dental.Framework.Shared.Caching;
using Dental.Framework.Shared.Identity;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.v1.Roles.UpdateRolePermissions;
using Dental.Modules.Identity.Data;
using Dental.Modules.Identity.Domain;
using Dental.Modules.Identity.Services;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Modules.Identity.Features.v1.Roles.UpdateRolePermissions;

/// <summary>Replaces the permission set of a role.</summary>
/// <param name="context">The identity context.</param>
/// <param name="cache">Shared cache, invalidated after the write.</param>
public sealed class UpdateRolePermissionsCommandHandler(IdentityModuleDbContext context, HybridCache cache)
    : ICommandHandler<UpdateRolePermissionsCommand, RoleDto>
{
    /// <inheritdoc />
    public async ValueTask<RoleDto> Handle(
        UpdateRolePermissionsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        DentalRole role = await context.Roles
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw NotFoundException.For("Role", command.RoleId);

        List<IdentityRoleClaim<Guid>> existing = await context.RoleClaims
            .Where(rc => rc.RoleId == role.Id && rc.ClaimType == DentalClaims.Permission)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        HashSet<string> target = command.Permissions.ToHashSet(StringComparer.Ordinal);

        context.RoleClaims.RemoveRange(
            existing.Where(rc => !target.Contains(rc.ClaimValue ?? string.Empty)));

        HashSet<string> current = existing
            .Select(rc => rc.ClaimValue ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string permission in target.Where(p => !current.Contains(p)))
        {
            context.RoleClaims.Add(UserService.ToRoleClaim(role.Id, permission));
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Every user holding this role now has a stale cached permission set.
        await cache.RemoveByTagAsync(CacheKeys.Tags.Identity, cancellationToken).ConfigureAwait(false);

        return new RoleDto(role.Id, role.Name!, role.Description, role.IsBuiltIn, command.Permissions);
    }
}
