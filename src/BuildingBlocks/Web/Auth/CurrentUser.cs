using System.Security.Claims;
using Dental.Framework.Core.Contracts;
using Dental.Framework.Shared.Identity;
using Microsoft.AspNetCore.Http;

namespace Dental.Framework.Web.Auth;

/// <summary>
/// Reads the caller from the HTTP request's principal.
/// </summary>
/// <remarks>
/// Do NOT inject this inside a SignalR hub: the negotiate <c>HttpContext</c> is not pinned to
/// subsequent hub invocations, so every property comes back null. Read <c>Context.User</c> there.
/// </remarks>
/// <param name="httpContextAccessor">Supplies the current request.</param>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : null;

    /// <inheritdoc />
    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    /// <inheritdoc />
    public string? TenantId => Principal?.FindFirstValue(DentalClaims.Tenant);

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles =>
        [.. Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? []];

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions =>
        [.. Principal?.FindAll(DentalClaims.Permission).Select(c => c.Value) ?? []];

    /// <inheritdoc />
    public bool HasPermission(string permission) =>
        Principal?.HasClaim(DentalClaims.Permission, permission) ?? false;
}
