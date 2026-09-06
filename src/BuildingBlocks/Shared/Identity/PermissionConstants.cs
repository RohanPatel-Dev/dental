using System.Collections.Immutable;

namespace Dental.Framework.Shared.Identity;

/// <summary>
/// Process wide registry of every permission any loaded module declares. Populated once per module
/// from <c>IModule.ConfigureServices</c>.
/// </summary>
/// <remarks>
/// The backing store is an immutable array swapped atomically, never a mutable collection
/// enumerated under concurrency - see the "static and global state" rule in the coding style guide.
/// </remarks>
public static class PermissionConstants
{
    private static ImmutableArray<Permission> _all = [];

    /// <summary>Every registered permission.</summary>
    public static ImmutableArray<Permission> All => _all;

    /// <summary>Permissions every authenticated tenant user holds implicitly.</summary>
    public static IEnumerable<Permission> Basic => _all.Where(p => p.IsBasic);

    /// <summary>Permissions only the root (operator) tenant may hold.</summary>
    public static IEnumerable<Permission> Root => _all.Where(p => p.IsRoot);

    /// <summary>Permissions available to a normal tenant administrator.</summary>
    public static IEnumerable<Permission> Admin => _all.Where(p => !p.IsRoot);

    /// <summary>
    /// Adds a module's permissions to the registry, ignoring duplicates so that a module loaded by
    /// two hosts in the same process is harmless.
    /// </summary>
    /// <param name="permissions">The module's declared permissions.</param>
    public static void Register(IEnumerable<Permission> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        Permission[] candidates = permissions as Permission[] ?? [.. permissions];

        ImmutableArray<Permission> snapshot;
        ImmutableArray<Permission> updated;
        do
        {
            snapshot = _all;
            HashSet<string> known = snapshot.Select(p => p.Value).ToHashSet(StringComparer.Ordinal);
            Permission[] additions = [.. candidates.Where(p => known.Add(p.Value))];
            if (additions.Length == 0)
            {
                return;
            }

            updated = snapshot.AddRange(additions);
        }
        while (!ImmutableInterlocked.InterlockedCompareExchange(ref _all, updated, snapshot).Equals(snapshot));
    }

    /// <summary>Clears the registry. Test only.</summary>
    public static void Reset() => ImmutableInterlocked.InterlockedExchange(ref _all, []);
}
