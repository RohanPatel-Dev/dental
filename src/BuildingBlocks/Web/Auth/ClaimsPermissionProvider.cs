using Dental.Framework.Core.Contracts;
using Dental.Framework.Shared.Identity;
using Microsoft.AspNetCore.Http;

namespace Dental.Framework.Web.Auth;

/// <summary>
/// Fallback permission provider, used until a module supplies a real one.
/// </summary>
/// <remarks>
/// Reads whatever permission claims happen to be on the principal, which is normally none - the
/// tokens this system issues carry roles only. A host that loads the Identity module gets that
/// module's cache backed provider instead, and this one exists so the authorization pipeline still
/// composes in a host that does not.
/// </remarks>
/// <param name="httpContextAccessor">Supplies the current principal.</param>
public sealed class ClaimsPermissionProvider(IHttpContextAccessor httpContextAccessor)
    : IPermissionProvider
{
    /// <inheritdoc />
    public Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> permissions =
        [
            .. httpContextAccessor.HttpContext?.User
                .FindAll(DentalClaims.Permission)
                .Select(c => c.Value) ?? [],
        ];

        return Task.FromResult(permissions);
    }
}
