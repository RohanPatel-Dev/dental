namespace Dental.Framework.Web.Auth;

/// <summary>The single implementation of <see cref="IRequiredPermissionMetadata"/>.</summary>
/// <param name="permission">The permission value the caller must hold.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequiredPermissionAttribute(string permission)
    : Attribute, IRequiredPermissionMetadata
{
    /// <inheritdoc />
    public string Permission { get; } = permission;
}
