namespace Dental.Framework.Shared.Identity;

/// <summary>Canonical verbs. Use these instead of inventing new spellings per module.</summary>
public static class PermissionActions
{
    /// <summary>Read a single resource or list resources.</summary>
    public const string View = nameof(View);

    /// <summary>Search or export a collection.</summary>
    public const string Search = nameof(Search);

    /// <summary>Create a new resource.</summary>
    public const string Create = nameof(Create);

    /// <summary>Modify an existing resource.</summary>
    public const string Update = nameof(Update);

    /// <summary>Remove a resource.</summary>
    public const string Delete = nameof(Delete);

    /// <summary>Export data out of the system.</summary>
    public const string Export = nameof(Export);

    /// <summary>Perform a privileged administrative operation.</summary>
    public const string Manage = nameof(Manage);
}
