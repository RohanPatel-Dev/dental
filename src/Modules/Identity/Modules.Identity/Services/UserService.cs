using System.Security.Claims;
using Dental.Framework.Caching;
using Dental.Framework.Shared.Caching;
using Dental.Framework.Shared.Identity;
using Dental.Modules.Identity.Contracts.Dtos;
using Dental.Modules.Identity.Contracts.Services;
using Dental.Modules.Identity.Data;
using Dental.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Dental.Modules.Identity.Services;

/// <summary>The Identity module's implementation of its own public contract.</summary>
/// <param name="context">The identity context.</param>
/// <param name="cache">Shared cache.</param>
public sealed class UserService(IdentityModuleDbContext context, HybridCache cache) : IUserService
{
    /// <inheritdoc />
    public async Task<UserDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        DentalUser? user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            return null;
        }

        IReadOnlyList<string> roles = await GetRoleNamesAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return Map(user, roles);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, UserDto>> GetManyAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, UserDto>();
        }

        List<DentalUser> users = await context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        Dictionary<Guid, List<string>> rolesByUser = await context.UserRoles
            .AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => x.Name ?? string.Empty).ToList(),
                cancellationToken)
            .ConfigureAwait(false);

        return users.ToDictionary(
            u => u.Id,
            u => Map(u, rolesByUser.TryGetValue(u.Id, out List<string>? roles) ? roles : []));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(
                CacheKeys.IdentityKeys.UserPermissions(userId),
                async token =>
                {
                    List<Guid> roleIds = await context.UserRoles
                        .AsNoTracking()
                        .Where(ur => ur.UserId == userId)
                        .Select(ur => ur.RoleId)
                        .ToListAsync(token)
                        .ConfigureAwait(false);

                    List<string> permissions = await context.RoleClaims
                        .AsNoTracking()
                        .Where(rc => roleIds.Contains(rc.RoleId) && rc.ClaimType == DentalClaims.Permission)
                        .Select(rc => rc.ClaimValue!)
                        .Distinct()
                        .ToListAsync(token)
                        .ConfigureAwait(false);

                    return (IReadOnlyList<string>)permissions;
                },
                tags: [CacheKeys.Tags.Identity],
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<long> CountActiveUsersAsync(
        string tenantId,
        CancellationToken cancellationToken = default) =>
        context.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId && u.IsActive && u.DeletedAt == null)
            .LongCountAsync(cancellationToken);

    /// <summary>Reads the role names held by one user.</summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role names.</returns>
    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        List<string> roles = await context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name ?? string.Empty)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return roles;
    }

    internal static UserDto Map(DentalUser user, IReadOnlyList<string> roles) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.IsActive,
            user.EmailConfirmed,
            roles,
            user.CreatedAt);

    internal static IEnumerable<Claim> ToPermissionClaims(IEnumerable<string> permissions) =>
        permissions.Select(p => new Claim(DentalClaims.Permission, p));

    internal static IdentityRoleClaim<Guid> ToRoleClaim(Guid roleId, string permission) =>
        new()
        {
            RoleId = roleId,
            ClaimType = DentalClaims.Permission,
            ClaimValue = permission,
        };
}
