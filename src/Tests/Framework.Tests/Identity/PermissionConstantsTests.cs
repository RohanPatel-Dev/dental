using System.Collections.Immutable;
using Dental.Framework.Shared.Identity;
using Shouldly;

namespace Dental.Framework.Tests.Identity;

/// <summary>
/// The permission registry is process-wide static state written from every module's
/// <c>ConfigureServices</c>. These tests pin the two properties that makes it safe: registration is
/// idempotent, and concurrent registration cannot lose entries.
/// </summary>
/// <remarks>
/// The class is not parallelized against itself because it mutates that shared registry.
/// </remarks>
[Collection(nameof(PermissionConstantsTests))]
public sealed class PermissionConstantsTests : IDisposable
{
    /// <summary>Starts from an empty registry so assertions are absolute, not relative.</summary>
    public PermissionConstantsTests() => PermissionConstants.Reset();

    #region Happy Path

    [Fact]
    public void Register_Should_AddTheModulesPermissions()
    {
        PermissionConstants.Register([Permission("View", "Patients"), Permission("Create", "Patients")]);

        PermissionConstants.All.Length.ShouldBe(2);
        PermissionConstants.All.Select(p => p.Value)
            .ShouldBe(["Permissions.Patients.View", "Permissions.Patients.Create"], ignoreOrder: true);
    }

    [Fact]
    public void Basic_Root_And_Admin_Should_PartitionTheRegistry()
    {
        PermissionConstants.Register(
        [
            Permission("View", "Patients", isBasic: true),
            Permission("Delete", "Patients"),
            Permission("Create", "Tenants", isRoot: true),
        ]);

        PermissionConstants.Basic.Select(p => p.Value).ShouldBe(["Permissions.Patients.View"]);
        PermissionConstants.Root.Select(p => p.Value).ShouldBe(["Permissions.Tenants.Create"]);
        PermissionConstants.Admin.Select(p => p.Value)
            .ShouldBe(["Permissions.Patients.View", "Permissions.Patients.Delete"], ignoreOrder: true);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Register_Should_IgnoreDuplicates_When_AModuleIsLoadedTwice()
    {
        // Two hosts in one process each call the module's ConfigureServices. That must not double
        // every permission in the role editor.
        Permission[] declared = [Permission("View", "Patients"), Permission("Create", "Patients")];

        PermissionConstants.Register(declared);
        PermissionConstants.Register(declared);

        PermissionConstants.All.Length.ShouldBe(2);
    }

    [Fact]
    public void Register_Should_AddOnlyTheNewEntries_When_TheSetsPartlyOverlap()
    {
        PermissionConstants.Register([Permission("View", "Patients")]);
        PermissionConstants.Register([Permission("View", "Patients"), Permission("Delete", "Patients")]);

        PermissionConstants.All.Length.ShouldBe(2);
    }

    [Fact]
    public void Register_Should_BeANoOp_ForAnEmptySet()
    {
        PermissionConstants.Register([]);

        PermissionConstants.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task Register_Should_LoseNothing_When_ModulesRegisterConcurrently()
    {
        // The compare-exchange loop is the whole reason the backing store is an ImmutableArray. A
        // plain List here would drop entries under exactly this load.
        const int Modules = 16;
        const int PerModule = 8;

        await Task.WhenAll(Enumerable.Range(0, Modules).Select(module => Task.Run(() =>
            PermissionConstants.Register(
                [.. Enumerable.Range(0, PerModule).Select(i => Permission($"Action{i}", $"Resource{module}"))]))));

        PermissionConstants.All.Length.ShouldBe(Modules * PerModule);
        PermissionConstants.All.Select(p => p.Value).Distinct(StringComparer.Ordinal)
            .Count().ShouldBe(Modules * PerModule);
    }

    [Fact]
    public void All_Should_ReturnASnapshotUnaffectedByLaterRegistrations()
    {
        PermissionConstants.Register([Permission("View", "Patients")]);
        ImmutableArray<Permission> snapshot = PermissionConstants.All;

        PermissionConstants.Register([Permission("Delete", "Patients")]);

        snapshot.Length.ShouldBe(1);
        PermissionConstants.All.Length.ShouldBe(2);
    }

    #endregion

    #region Exception Cases

    [Fact]
    public void Register_Should_Throw_When_ThePermissionsAreNull() =>
        Should.Throw<ArgumentNullException>(() => PermissionConstants.Register(null!));

    #endregion

    /// <summary>Leaves the registry empty for whatever runs next.</summary>
    public void Dispose() => PermissionConstants.Reset();

    private static Permission Permission(
        string action,
        string resource,
        bool isBasic = false,
        bool isRoot = false) =>
        new(action + " " + resource, action, resource, resource, isBasic, isRoot);
}
