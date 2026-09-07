using System.Reflection;
using Dental.Framework.Web.Auth;
using Shouldly;

namespace Dental.Architecture.Tests.Rules;

/// <summary>
/// Guards the silent gate-disabler: a second type implementing the permission metadata contract.
/// </summary>
/// <remarks>
/// The authorization handler matches endpoint metadata by interface. A duplicate interface - a copy
/// pasted into a module, or a second definition in another assembly - means the handler matches
/// none of the endpoints, every permission check passes, and nothing anywhere reports an error.
/// </remarks>
public sealed class AuthorizationMetadataTests
{
    #region Happy Path

    [Fact]
    public void ExactlyOneTypeShouldImplementRequiredPermissionMetadata()
    {
        Type[] implementations =
        [
            .. AllAssemblies()
                .SelectMany(SafeGetTypes)
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .Where(t => t.IsAssignableTo(typeof(IRequiredPermissionMetadata))),
        ];

        implementations.Length.ShouldBe(
            1,
            "There must be exactly one implementation of IRequiredPermissionMetadata. A duplicate "
            + "silently disables EVERY permission gate in the API. Found: "
            + string.Join(", ", implementations.Select(t => t.FullName)));

        implementations[0].ShouldBe(typeof(RequiredPermissionAttribute));
    }

    [Fact]
    public void OnlyOneInterfaceNamedRequiredPermissionMetadataShouldExist()
    {
        Type[] interfaces =
        [
            .. AllAssemblies()
                .SelectMany(SafeGetTypes)
                .Where(t => t.IsInterface
                            && string.Equals(
                                t.Name,
                                nameof(IRequiredPermissionMetadata),
                                StringComparison.Ordinal)),
        ];

        interfaces.Length.ShouldBe(
            1,
            "A second interface with this name, in any assembly, is the failure this test exists "
            + "for: endpoint metadata would be tagged with one and read through the other. Found: "
            + string.Join(", ", interfaces.Select(t => t.AssemblyQualifiedName)));
    }

    [Fact]
    public void EveryDeclaredPermission_Should_HaveAUniqueValue()
    {
        // Two permissions sharing a value make one of them unenforceable, because the role editor
        // and the gate both key on the value.
        RegisterAllModulePermissions();

        string[] duplicates =
        [
            .. Framework.Shared.Identity.PermissionConstants.All
                .GroupBy(p => p.Value, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key),
        ];

        duplicates.ShouldBeEmpty("Duplicate permission values: " + string.Join(", ", duplicates));
    }

    #endregion

    private static void RegisterAllModulePermissions()
    {
        Framework.Shared.Identity.PermissionConstants.Reset();

        Framework.Shared.Identity.PermissionConstants.Register(
            Modules.Tenancy.Contracts.Authorization.TenancyPermissions.All);
        Framework.Shared.Identity.PermissionConstants.Register(
            Modules.Identity.Contracts.Authorization.IdentityPermissions.All);
        Framework.Shared.Identity.PermissionConstants.Register(
            Modules.Auditing.Contracts.Authorization.AuditingPermissions.All);
        Framework.Shared.Identity.PermissionConstants.Register(
            Modules.Patients.Contracts.Authorization.PatientsPermissions.All);
        Framework.Shared.Identity.PermissionConstants.Register(
            Modules.Scheduling.Contracts.Authorization.SchedulingPermissions.All);
        Framework.Shared.Identity.PermissionConstants.Register(
            Modules.Clinical.Contracts.Authorization.ClinicalPermissions.All);
        Framework.Shared.Identity.PermissionConstants.Register(
            Modules.Billing.Contracts.Authorization.BillingPermissions.All);
        Framework.Shared.Identity.PermissionConstants.Register(
            Modules.Notifications.Contracts.Authorization.NotificationsPermissions.All);
    }

    private static IEnumerable<Assembly> AllAssemblies() =>
        ArchitectureFixture.ModuleAssemblies
            .Concat(ArchitectureFixture.ContractsAssemblies)
            .Concat(ArchitectureFixture.FrameworkAssemblies)
            .Distinct();

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
