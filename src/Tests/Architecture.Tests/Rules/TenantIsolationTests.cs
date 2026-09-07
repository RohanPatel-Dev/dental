using System.Reflection;
using Dental.Framework.Core.Domain;
using Shouldly;

namespace Dental.Architecture.Tests.Rules;

/// <summary>
/// Tenant isolation is default-ON. These rules make the opt-out deliberate and visible, because the
/// failure mode is one practice reading another's patient records.
/// </summary>
public sealed class TenantIsolationTests
{
    #region Happy Path

    [Fact]
    public void EveryPersistedEntity_Should_BeTenantScopedOrExplicitlyGlobal()
    {
        List<string> offending =
        [
            .. EntityTypes()
                .Where(t => !t.IsAssignableTo(typeof(IHasTenant))
                            && !t.IsAssignableTo(typeof(IGlobalEntity)))
                .Select(t => t.FullName!),
        ];

        offending.ShouldBeEmpty(
            "An entity is either tenant scoped (IHasTenant, which BaseEntity supplies) or "
            + "deliberately global (IGlobalEntity). Anything else has no query filter and no reason "
            + "for not having one. Offending: " + string.Join(", ", offending));
    }

    [Fact]
    public void GlobalEntities_Should_BeAShortKnownList()
    {
        // Every addition here removes a tenant filter, so the list is asserted rather than counted:
        // adding to it should require editing this test and explaining why.
        string[] expected =
        [
            "InboxMessage",
            "OutboxMessage",
            "Tenant",
            "TenantPlan",
        ];

        string[] actual =
        [
            .. EntityTypes()
                .Where(t => t.IsAssignableTo(typeof(IGlobalEntity)))
                .Select(t => t.Name)
                .Distinct()
                .Order(StringComparer.Ordinal),
        ];

        actual.ShouldBe(
            expected,
            "The set of entities that opt out of tenant filtering changed. Every one of these is a "
            + "row any tenant can see: confirm that is intended, then update this list.");
    }

    #endregion

    #region Exception Cases

    [Fact]
    public void ModuleDbContexts_Should_DeriveFromBaseDbContext_OrDocumentWhyNot()
    {
        // BaseDbContext is what applies the conventions. The Identity context cannot derive from it
        // - ASP.NET Identity requires IdentityDbContext and C# has no multiple inheritance - so it
        // applies them by hand and is named here as the single sanctioned exception.
        string[] sanctionedExceptions = ["IdentityModuleDbContext"];

        List<string> offending =
        [
            .. ArchitectureFixture.ModuleAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsAbstract: false } && t.IsAssignableTo(typeof(Microsoft.EntityFrameworkCore.DbContext)))
                .Where(t => !t.IsAssignableTo(typeof(Framework.Persistence.Contexts.BaseDbContext)))
                .Where(t => !sanctionedExceptions.Contains(t.Name, StringComparer.Ordinal))
                .Select(t => t.FullName!),
        ];

        offending.ShouldBeEmpty(
            "A module DbContext must derive from BaseDbContext, which applies tenant and soft-delete "
            + "filters. If it genuinely cannot, apply ModelConventions.ApplyDentalConventions by hand "
            + "and add it to the sanctioned list here with a reason. Offending: "
            + string.Join(", ", offending));
    }

    #endregion

    private static IEnumerable<Type> EntityTypes() =>
        ArchitectureFixture.ModuleAssemblies
            .Concat(ArchitectureFixture.FrameworkAssemblies)
            .Distinct()
            .SelectMany(SafeGetTypes)
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.IsAssignableTo(typeof(BaseEntity))
                        || t.IsAssignableTo(typeof(IGlobalEntity)));

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
