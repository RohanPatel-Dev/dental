namespace Dental.Framework.Core.Contracts;

/// <summary>
/// Resolves the effective permissions of an authenticated user.
/// </summary>
/// <remarks>
/// The access token deliberately carries ROLE names only, never the expanded permission set: a token
/// cannot be un-issued, so embedding permissions would mean a revoked one stays effective until the
/// token expires. Server-side authorization therefore resolves permissions per request through this
/// abstraction, and the Identity module's implementation serves them from the cache.
/// </remarks>
public interface IPermissionProvider
{
    /// <summary>Reads the permissions a user effectively holds.</summary>
    /// <param name="userId">The authenticated user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The permission values.</returns>
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
