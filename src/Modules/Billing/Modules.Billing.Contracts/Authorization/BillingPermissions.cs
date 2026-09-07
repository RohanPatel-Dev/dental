using Dental.Framework.Shared.Identity;

namespace Dental.Modules.Billing.Contracts.Authorization;

/// <summary>Permissions this module declares.</summary>
public static class BillingPermissions
{
    /// <summary>Grouping shown in the role editor.</summary>
    public const string Group = "Billing";

    /// <summary>Invoices.</summary>
    public static class Invoices
    {
        /// <summary>Resource name.</summary>
        public const string Resource = "Invoices";

        /// <summary>Read an invoice.</summary>
        public const string View = $"Permissions.{Resource}.{PermissionActions.View}";

        /// <summary>Page through invoices.</summary>
        public const string Search = $"Permissions.{Resource}.{PermissionActions.Search}";

        /// <summary>Issue a draft invoice.</summary>
        public const string Create = $"Permissions.{Resource}.{PermissionActions.Create}";

        /// <summary>Void an invoice.</summary>
        public const string Update = $"Permissions.{Resource}.{PermissionActions.Update}";
    }

    /// <summary>Payments.</summary>
    public static class Payments
    {
        /// <summary>Resource name.</summary>
        public const string Resource = "Payments";

        /// <summary>Read a payment.</summary>
        public const string View = $"Permissions.{Resource}.{PermissionActions.View}";

        /// <summary>Take a payment.</summary>
        public const string Create = $"Permissions.{Resource}.{PermissionActions.Create}";
    }

    /// <summary>Everything this module declares.</summary>
    public static IReadOnlyList<Permission> All { get; } =
    [
        new("View invoices", PermissionActions.View, Invoices.Resource, Group, false, false),
        new("Search invoices", PermissionActions.Search, Invoices.Resource, Group, false, false),
        new("Issue invoices", PermissionActions.Create, Invoices.Resource, Group, false, false),
        new("Void invoices", PermissionActions.Update, Invoices.Resource, Group, false, false),
        new("View payments", PermissionActions.View, Payments.Resource, Group, false, false),
        new("Take payments", PermissionActions.Create, Payments.Resource, Group, false, false),
    ];
}
