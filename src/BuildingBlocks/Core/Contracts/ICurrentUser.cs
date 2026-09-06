namespace Dental.Framework.Core.Contracts;

/// <summary>
/// Ambient information about the caller of the current request.
/// Never inject this inside a SignalR hub - read <c>Context.User</c> there instead, because the
/// negotiate <c>HttpContext</c> is not pinned to subsequent hub invocations.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Authenticated user identifier, or <see langword="null"/> for anonymous callers.</summary>
    Guid? UserId { get; }

    /// <summary>Authenticated user's email address.</summary>
    string? Email { get; }

    /// <summary>Tenant resolved for this request.</summary>
    string? TenantId { get; }

    /// <summary>True when the request carries an authenticated principal.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Role names carried by the token.</summary>
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>Fine grained permissions granted to the caller.</summary>
    IReadOnlyCollection<string> Permissions { get; }

    /// <summary>Checks a single permission.</summary>
    /// <param name="permission">Permission constant, e.g. <c>Permissions.Patients.View</c>.</param>
    /// <returns><see langword="true"/> when the caller holds it.</returns>
    bool HasPermission(string permission);
}
