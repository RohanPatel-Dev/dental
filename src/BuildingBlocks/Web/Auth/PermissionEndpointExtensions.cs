using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Dental.Framework.Web.Auth;

/// <summary>Attaches permission metadata to an endpoint.</summary>
public static class PermissionEndpointExtensions
{
    /// <summary>
    /// Requires the caller to hold a permission. Adds authorization if the endpoint did not already
    /// require it.
    /// </summary>
    /// <param name="builder">The endpoint builder.</param>
    /// <param name="permission">Permission constant, from a module's Authorization folder.</param>
    /// <returns>The endpoint builder, for chaining.</returns>
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder,
        string permission)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return builder
            .WithMetadata(new RequiredPermissionAttribute(permission))
            .RequireAuthorization(AuthorizationPolicies.Permission);
    }

    /// <summary>Requires a permission on every endpoint in a group.</summary>
    /// <param name="builder">The group builder.</param>
    /// <param name="permission">Permission constant.</param>
    /// <returns>The group builder, for chaining.</returns>
    public static RouteGroupBuilder RequirePermission(
        this RouteGroupBuilder builder,
        string permission)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return builder
            .WithMetadata(new RequiredPermissionAttribute(permission))
            .RequireAuthorization(AuthorizationPolicies.Permission);
    }
}
