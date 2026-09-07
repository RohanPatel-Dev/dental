using Dental.Framework.Shared.Identity;

namespace Dental.Modules.Patients.Contracts.Authorization;

/// <summary>Permissions this module declares.</summary>
public static class PatientsPermissions
{
    /// <summary>Grouping shown in the role editor.</summary>
    public const string Group = "Patients";

    /// <summary>Resource these permissions apply to.</summary>
    public const string Resource = "Patients";

    /// <summary>Read a patient record.</summary>
    public const string View = $"Permissions.{Resource}.{PermissionActions.View}";

    /// <summary>Page through patient records.</summary>
    public const string Search = $"Permissions.{Resource}.{PermissionActions.Search}";

    /// <summary>Register a patient.</summary>
    public const string Create = $"Permissions.{Resource}.{PermissionActions.Create}";

    /// <summary>Amend a patient record.</summary>
    public const string Update = $"Permissions.{Resource}.{PermissionActions.Update}";

    /// <summary>Erase a patient record.</summary>
    public const string Delete = $"Permissions.{Resource}.{PermissionActions.Delete}";

    /// <summary>Export patient data, for a subject access request.</summary>
    public const string Export = $"Permissions.{Resource}.{PermissionActions.Export}";

    /// <summary>Everything this module declares.</summary>
    public static IReadOnlyList<Permission> All { get; } =
    [
        new("View patients", PermissionActions.View, Resource, Group, IsBasic: false, IsRoot: false),
        new("Search patients", PermissionActions.Search, Resource, Group, IsBasic: false, IsRoot: false),
        new("Register patients", PermissionActions.Create, Resource, Group, IsBasic: false, IsRoot: false),
        new("Update patients", PermissionActions.Update, Resource, Group, IsBasic: false, IsRoot: false),
        new("Erase patients", PermissionActions.Delete, Resource, Group, IsBasic: false, IsRoot: false),
        new("Export patient data", PermissionActions.Export, Resource, Group, IsBasic: false, IsRoot: false),
    ];
}
