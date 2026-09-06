using Dental.Framework.Shared.Identity;

namespace Dental.Modules.Tenancy.Contracts.Authorization;

/// <summary>Permissions this module declares. Registered from <c>TenancyModule.ConfigureServices</c>.</summary>
public static class TenancyPermissions
{
    /// <summary>Grouping shown in the role editor.</summary>
    public const string Group = "Tenancy";

    /// <summary>Resource these permissions apply to.</summary>
    public const string Resource = "Tenants";

    /// <summary>View a tenant.</summary>
    public const string View = $"Permissions.{Resource}.{PermissionActions.View}";

    /// <summary>Search the tenant catalog.</summary>
    public const string Search = $"Permissions.{Resource}.{PermissionActions.Search}";

    /// <summary>Create a tenant.</summary>
    public const string Create = $"Permissions.{Resource}.{PermissionActions.Create}";

    /// <summary>Update a tenant.</summary>
    public const string Update = $"Permissions.{Resource}.{PermissionActions.Update}";

    /// <summary>Activate, deactivate or change a tenant's plan.</summary>
    public const string Manage = $"Permissions.{Resource}.{PermissionActions.Manage}";

    /// <summary>Everything this module declares. Operator-only: the catalog is cross-tenant.</summary>
    public static IReadOnlyList<Permission> All { get; } =
    [
        new("View tenants", PermissionActions.View, Resource, Group, IsBasic: false, IsRoot: true),
        new("Search tenants", PermissionActions.Search, Resource, Group, IsBasic: false, IsRoot: true),
        new("Create tenants", PermissionActions.Create, Resource, Group, IsBasic: false, IsRoot: true),
        new("Update tenants", PermissionActions.Update, Resource, Group, IsBasic: false, IsRoot: true),
        new("Manage tenants", PermissionActions.Manage, Resource, Group, IsBasic: false, IsRoot: true),
    ];
}
