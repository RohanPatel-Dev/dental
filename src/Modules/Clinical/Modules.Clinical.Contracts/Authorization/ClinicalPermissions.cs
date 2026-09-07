using Dental.Framework.Shared.Identity;

namespace Dental.Modules.Clinical.Contracts.Authorization;

/// <summary>Permissions this module declares.</summary>
public static class ClinicalPermissions
{
    /// <summary>Grouping shown in the role editor.</summary>
    public const string Group = "Clinical";

    /// <summary>The procedure catalog.</summary>
    public static class Procedures
    {
        /// <summary>Resource name.</summary>
        public const string Resource = "Procedures";

        /// <summary>Read the catalog.</summary>
        public const string View = $"Permissions.{Resource}.{PermissionActions.View}";

        /// <summary>Add a procedure to the catalog.</summary>
        public const string Create = $"Permissions.{Resource}.{PermissionActions.Create}";

        /// <summary>Change a catalogued procedure.</summary>
        public const string Update = $"Permissions.{Resource}.{PermissionActions.Update}";
    }

    /// <summary>Treatment plans and the tooth chart.</summary>
    public static class Treatment
    {
        /// <summary>Resource name.</summary>
        public const string Resource = "Treatment";

        /// <summary>Read a treatment plan or chart.</summary>
        public const string View = $"Permissions.{Resource}.{PermissionActions.View}";

        /// <summary>Page through treatment plans.</summary>
        public const string Search = $"Permissions.{Resource}.{PermissionActions.Search}";

        /// <summary>Author a treatment plan or chart entry.</summary>
        public const string Create = $"Permissions.{Resource}.{PermissionActions.Create}";

        /// <summary>Amend a treatment plan.</summary>
        public const string Update = $"Permissions.{Resource}.{PermissionActions.Update}";
    }

    /// <summary>Everything this module declares.</summary>
    public static IReadOnlyList<Permission> All { get; } =
    [
        new("View procedures", PermissionActions.View, Procedures.Resource, Group, false, false),
        new("Add procedures", PermissionActions.Create, Procedures.Resource, Group, false, false),
        new("Update procedures", PermissionActions.Update, Procedures.Resource, Group, false, false),
        new("View treatment", PermissionActions.View, Treatment.Resource, Group, false, false),
        new("Search treatment", PermissionActions.Search, Treatment.Resource, Group, false, false),
        new("Author treatment", PermissionActions.Create, Treatment.Resource, Group, false, false),
        new("Amend treatment", PermissionActions.Update, Treatment.Resource, Group, false, false),
    ];
}
