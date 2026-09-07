using System.Security.Claims;
using Dental.Framework.Core.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Dental.Framework.Web.Auth;

/// <summary>Requirement satisfied by any endpoint whose metadata permissions the caller holds.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement;

/// <summary>
/// Reads <see cref="IRequiredPermissionMetadata"/> off the matched endpoint and checks it against
/// the caller's effective permissions.
/// </summary>
/// <remarks>
/// Permissions are RESOLVED per request rather than read from the token, because the token carries
/// role names only. The Identity module's provider serves them from the cache, so this costs a cache
/// hit rather than a query on the hot path - and a permission removed from a role takes effect on
/// the next request instead of at the next token refresh.
/// </remarks>
/// <param name="httpContextAccessor">Supplies the matched endpoint.</param>
/// <param name="permissionProvider">Resolves the caller's effective permissions.</param>
public sealed class PermissionAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    IPermissionProvider permissionProvider) : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);

        Endpoint? endpoint = httpContextAccessor.HttpContext?.GetEndpoint();

        IReadOnlyList<IRequiredPermissionMetadata> required =
            endpoint?.Metadata.GetOrderedMetadata<IRequiredPermissionMetadata>() ?? [];

        if (required.Count == 0)
        {
            // No permission declared: the endpoint is gated by authentication alone.
            context.Succeed(requirement);
            return;
        }

        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId))
        {
            return;
        }

        CancellationToken cancellationToken =
            httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        IReadOnlyCollection<string> permissions = await permissionProvider
            .GetPermissionsAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        HashSet<string> held = permissions.ToHashSet(StringComparer.Ordinal);

        if (required.All(r => held.Contains(r.Permission)))
        {
            context.Succeed(requirement);
        }
    }
}

/// <summary>Policy names used by the platform.</summary>
public static class AuthorizationPolicies
{
    /// <summary>Default policy: authenticated, plus any endpoint declared permissions.</summary>
    public const string Permission = nameof(Permission);
}
