namespace Dental.Framework.Shared.Identity;

/// <summary>
/// A single fine grained permission. Modules declare their own set in their Contracts project and
/// hand it to <see cref="PermissionConstants.Register"/> from <c>ConfigureServices</c>.
/// </summary>
/// <param name="Name">Display name shown in the role editor.</param>
/// <param name="Action">Verb, e.g. <c>View</c>, <c>Create</c>, <c>Update</c>, <c>Delete</c>.</param>
/// <param name="Resource">Resource the action applies to, e.g. <c>Patients</c>.</param>
/// <param name="Group">Grouping used by the role editor UI, usually the module name.</param>
/// <param name="IsBasic">True when every authenticated user in the tenant holds it.</param>
/// <param name="IsRoot">True when only the root (operator) tenant may hold it.</param>
public sealed record Permission(
    string Name,
    string Action,
    string Resource,
    string Group,
    bool IsBasic,
    bool IsRoot)
{
    /// <summary>The claim value, of the form <c>Permissions.{Resource}.{Action}</c>.</summary>
    public string Value => $"Permissions.{Resource}.{Action}";
}
