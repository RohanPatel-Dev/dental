using Dental.Modules.Identity.Contracts.Dtos;

namespace Dental.Modules.Identity.Contracts.Services;

/// <summary>
/// The Identity module's public surface. Other modules inject this to resolve a user's display name
/// or permission set - never the Identity DbContext.
/// </summary>
public interface IUserService
{
    /// <summary>Reads one user in the current tenant.</summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user, or null when they do not exist in this tenant.</returns>
    Task<UserDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Reads several users at once, for list projections.</summary>
    /// <param name="userIds">User identifiers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The users that exist, keyed by identifier.</returns>
    Task<IReadOnlyDictionary<Guid, UserDto>> GetManyAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>Reads a user's effective permissions, unioned across their roles.</summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The permission values.</returns>
    Task<IReadOnlyList<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Counts active users in a tenant. Used by the quota gauge.</summary>
    /// <param name="tenantId">Tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of active users.</returns>
    Task<long> CountActiveUsersAsync(string tenantId, CancellationToken cancellationToken = default);
}
