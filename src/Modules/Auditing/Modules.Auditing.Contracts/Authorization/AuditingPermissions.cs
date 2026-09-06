using Dental.Framework.Shared.Identity;

namespace Dental.Modules.Auditing.Contracts.Authorization;

/// <summary>Permissions this module declares.</summary>
public static class AuditingPermissions
{
    /// <summary>Grouping shown in the role editor.</summary>
    public const string Group = "Auditing";

    /// <summary>Resource these permissions apply to.</summary>
    public const string Resource = "AuditTrails";

    /// <summary>Read the audit trail.</summary>
    public const string View = $"Permissions.{Resource}.{PermissionActions.View}";

    /// <summary>Page through the audit trail.</summary>
    public const string Search = $"Permissions.{Resource}.{PermissionActions.Search}";

    /// <summary>Export the audit trail.</summary>
    public const string Export = $"Permissions.{Resource}.{PermissionActions.Export}";

    /// <summary>Everything this module declares.</summary>
    public static IReadOnlyList<Permission> All { get; } =
    [
        new("View audit trail", PermissionActions.View, Resource, Group, IsBasic: false, IsRoot: false),
        new("Search audit trail", PermissionActions.Search, Resource, Group, IsBasic: false, IsRoot: false),
        new("Export audit trail", PermissionActions.Export, Resource, Group, IsBasic: false, IsRoot: false),
    ];
}
