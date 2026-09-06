using Dental.Framework.Shared.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Dental.Framework.Web.Auth;

/// <summary>Requirement satisfied by any endpoint whose metadata permissions the caller holds.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement;

/// <summary>
/// Reads <see cref="IRequiredPermissionMetadata"/> off the matched endpoint and checks the caller's
/// permission claims.
/// </summary>
/// <param name="httpContextAccessor">Supplies the matched endpoint.</param>
public sealed class PermissionAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
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
            return Task.CompletedTask;
        }

        HashSet<string> held = context.User
            .FindAll(DentalClaims.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        if (required.All(r => held.Contains(r.Permission)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>Policy names used by the platform.</summary>
public static class AuthorizationPolicies
{
    /// <summary>Default policy: authenticated, plus any endpoint declared permissions.</summary>
    public const string Permission = nameof(Permission);
}
