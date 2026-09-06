using Dental.Framework.Shared.Identity;

namespace Dental.Modules.Identity.Contracts.Authorization;

/// <summary>Permissions this module declares.</summary>
public static class IdentityPermissions
{
    /// <summary>Grouping shown in the role editor.</summary>
    public const string Group = "Identity";

    /// <summary>Users.</summary>
    public static class Users
    {
        /// <summary>Resource name.</summary>
        public const string Resource = "Users";

        /// <summary>Read a user.</summary>
        public const string View = $"Permissions.{Resource}.{PermissionActions.View}";

        /// <summary>Page through users.</summary>
        public const string Search = $"Permissions.{Resource}.{PermissionActions.Search}";

        /// <summary>Invite a user.</summary>
        public const string Create = $"Permissions.{Resource}.{PermissionActions.Create}";

        /// <summary>Change a user's profile or status.</summary>
        public const string Update = $"Permissions.{Resource}.{PermissionActions.Update}";

        /// <summary>Remove a user.</summary>
        public const string Delete = $"Permissions.{Resource}.{PermissionActions.Delete}";
    }

    /// <summary>Roles.</summary>
    public static class Roles
    {
        /// <summary>Resource name.</summary>
        public const string Resource = "Roles";

        /// <summary>Read a role.</summary>
        public const string View = $"Permissions.{Resource}.{PermissionActions.View}";

        /// <summary>Create a role.</summary>
        public const string Create = $"Permissions.{Resource}.{PermissionActions.Create}";

        /// <summary>Change a role's permissions.</summary>
        public const string Update = $"Permissions.{Resource}.{PermissionActions.Update}";

        /// <summary>Delete a role.</summary>
        public const string Delete = $"Permissions.{Resource}.{PermissionActions.Delete}";
    }

    /// <summary>Everything this module declares.</summary>
    public static IReadOnlyList<Permission> All { get; } =
    [
        new("View users", PermissionActions.View, Users.Resource, Group, IsBasic: false, IsRoot: false),
        new("Search users", PermissionActions.Search, Users.Resource, Group, IsBasic: false, IsRoot: false),
        new("Invite users", PermissionActions.Create, Users.Resource, Group, IsBasic: false, IsRoot: false),
        new("Update users", PermissionActions.Update, Users.Resource, Group, IsBasic: false, IsRoot: false),
        new("Delete users", PermissionActions.Delete, Users.Resource, Group, IsBasic: false, IsRoot: false),
        new("View roles", PermissionActions.View, Roles.Resource, Group, IsBasic: false, IsRoot: false),
        new("Create roles", PermissionActions.Create, Roles.Resource, Group, IsBasic: false, IsRoot: false),
        new("Update roles", PermissionActions.Update, Roles.Resource, Group, IsBasic: false, IsRoot: false),
        new("Delete roles", PermissionActions.Delete, Roles.Resource, Group, IsBasic: false, IsRoot: false),
    ];
}
