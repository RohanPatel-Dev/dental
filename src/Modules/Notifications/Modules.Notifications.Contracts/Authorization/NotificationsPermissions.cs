using Dental.Framework.Shared.Identity;

namespace Dental.Modules.Notifications.Contracts.Authorization;

/// <summary>Permissions this module declares.</summary>
public static class NotificationsPermissions
{
    /// <summary>Grouping shown in the role editor.</summary>
    public const string Group = "Notifications";

    /// <summary>Resource these permissions apply to.</summary>
    public const string Resource = "Notifications";

    /// <summary>Read a notification.</summary>
    public const string View = $"Permissions.{Resource}.{PermissionActions.View}";

    /// <summary>Page through the outbound log.</summary>
    public const string Search = $"Permissions.{Resource}.{PermissionActions.Search}";

    /// <summary>Everything this module declares.</summary>
    public static IReadOnlyList<Permission> All { get; } =
    [
        new("View notifications", PermissionActions.View, Resource, Group, false, false),
        new("Search notifications", PermissionActions.Search, Resource, Group, false, false),
    ];
}
