using Dental.Framework.Shared.Identity;

namespace Dental.Modules.Scheduling.Contracts.Authorization;

/// <summary>Permissions this module declares.</summary>
public static class SchedulingPermissions
{
    /// <summary>Grouping shown in the role editor.</summary>
    public const string Group = "Scheduling";

    /// <summary>Appointments.</summary>
    public static class Appointments
    {
        /// <summary>Resource name.</summary>
        public const string Resource = "Appointments";

        /// <summary>Read an appointment.</summary>
        public const string View = $"Permissions.{Resource}.{PermissionActions.View}";

        /// <summary>Page through the appointment book.</summary>
        public const string Search = $"Permissions.{Resource}.{PermissionActions.Search}";

        /// <summary>Book an appointment.</summary>
        public const string Create = $"Permissions.{Resource}.{PermissionActions.Create}";

        /// <summary>Reschedule, cancel or complete an appointment.</summary>
        public const string Update = $"Permissions.{Resource}.{PermissionActions.Update}";
    }

    /// <summary>Providers and operatories.</summary>
    public static class Providers
    {
        /// <summary>Resource name.</summary>
        public const string Resource = "Providers";

        /// <summary>Read a provider.</summary>
        public const string View = $"Permissions.{Resource}.{PermissionActions.View}";

        /// <summary>Add a provider.</summary>
        public const string Create = $"Permissions.{Resource}.{PermissionActions.Create}";

        /// <summary>Change a provider's details or working hours.</summary>
        public const string Update = $"Permissions.{Resource}.{PermissionActions.Update}";
    }

    /// <summary>Everything this module declares.</summary>
    public static IReadOnlyList<Permission> All { get; } =
    [
        new("View appointments", PermissionActions.View, Appointments.Resource, Group, false, false),
        new("Search appointments", PermissionActions.Search, Appointments.Resource, Group, false, false),
        new("Book appointments", PermissionActions.Create, Appointments.Resource, Group, false, false),
        new("Update appointments", PermissionActions.Update, Appointments.Resource, Group, false, false),
        new("View providers", PermissionActions.View, Providers.Resource, Group, false, false),
        new("Add providers", PermissionActions.Create, Providers.Resource, Group, false, false),
        new("Update providers", PermissionActions.Update, Providers.Resource, Group, false, false),
    ];
}
