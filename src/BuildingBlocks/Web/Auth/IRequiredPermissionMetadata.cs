namespace Dental.Framework.Web.Auth;

/// <summary>
/// Endpoint metadata naming the permission an endpoint requires.
/// </summary>
/// <remarks>
/// There must be EXACTLY ONE type in the process implementing this contract. A duplicate definition
/// - a second copy in another assembly, or a module redeclaring it - makes the authorization
/// handler match none of the endpoints and silently disables every permission gate in the API.
/// <c>AuthorizationMetadataTests</c> exists to catch that.
/// </remarks>
public interface IRequiredPermissionMetadata
{
    /// <summary>The permission value the caller must hold.</summary>
    string Permission { get; }
}
