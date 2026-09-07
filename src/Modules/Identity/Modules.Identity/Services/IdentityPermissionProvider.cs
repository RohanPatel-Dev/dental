using Dental.Framework.Core.Contracts;
using Dental.Modules.Identity.Contracts.Services;

namespace Dental.Modules.Identity.Services;

/// <summary>
/// Resolves a caller's permissions by expanding the roles they hold, served from the shared cache.
/// </summary>
/// <remarks>
/// This is what makes the authorization gate work at all: the access token carries role names only,
/// so without a provider the handler would find no permissions and refuse every gated endpoint.
/// </remarks>
/// <param name="userService">Reads the cached permission set.</param>
public sealed class IdentityPermissionProvider(IUserService userService) : IPermissionProvider
{
    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await userService.GetPermissionsAsync(userId, cancellationToken).ConfigureAwait(false);
}
