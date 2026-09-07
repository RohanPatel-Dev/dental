using System.Reflection;
using Dental.Framework.Web.Modules;
using Shouldly;

namespace Dental.Architecture.Tests.Rules;

/// <summary>
/// Guards the registration footgun: a module is declared in several places, and every one of them
/// fails silently when missed.
/// </summary>
public sealed class ModuleRegistrationTests
{
    #region Happy Path

    [Fact]
    public void EveryModuleAssembly_Should_DeclareExactlyOneFshModuleAttribute()
    {
        foreach (Assembly module in ArchitectureFixture.ModuleAssemblies)
        {
            FshModuleAttribute[] attributes = [.. module.GetCustomAttributes<FshModuleAttribute>()];

            attributes.Length.ShouldBe(
                1,
                $"{module.GetName().Name} should declare exactly one [assembly: FshModule(...)]. "
                + "The loader scans ASSEMBLY attributes only - a class-level attribute is ignored "
                + "and the module simply never loads.");
        }
    }

    [Fact]
    public void EveryDeclaredModuleType_Should_ImplementIModule()
    {
        foreach (Assembly module in ArchitectureFixture.ModuleAssemblies)
        {
            foreach (FshModuleAttribute attribute in module.GetCustomAttributes<FshModuleAttribute>())
            {
                attribute.ModuleType.IsAssignableTo(typeof(IModule)).ShouldBeTrue(
                    $"{attribute.ModuleType.FullName} is declared with [FshModule] but does not "
                    + "implement IModule.");

                attribute.ModuleType.GetConstructor(Type.EmptyTypes).ShouldNotBeNull(
                    $"{attribute.ModuleType.FullName} needs a public parameterless constructor: the "
                    + "loader instantiates it before any container exists.");
            }
        }
    }

    [Fact]
    public void ModuleLoadOrders_Should_BeUnique()
    {
        var declarations = ArchitectureFixture.ModuleAssemblies
            .SelectMany(a => a.GetCustomAttributes<FshModuleAttribute>())
            .Select(a => new { a.ModuleType.Name, a.Order })
            .ToList();

        string[] duplicates =
        [
            .. declarations
                .GroupBy(d => d.Order)
                .Where(g => g.Count() > 1)
                .Select(g => $"{g.Key}: {string.Join(", ", g.Select(d => d.Name))}"),
        ];

        // Ties break by type name, so a duplicate is not fatal - but it makes load order depend on
        // alphabetical accident, which is not something to discover during an incident.
        duplicates.ShouldBeEmpty(
            "Two modules share a load order: " + string.Join(" | ", duplicates));
    }

    [Fact]
    public void EveryContractsAssembly_Should_ExposeAMarkerType()
    {
        foreach (Assembly contracts in ArchitectureFixture.ContractsAssemblies)
        {
            Type[] markers =
            [
                .. contracts.GetTypes()
                    .Where(t => t.Name.EndsWith("ContractsMarker", StringComparison.Ordinal)),
            ];

            markers.Length.ShouldBe(
                1,
                $"{contracts.GetName().Name} needs exactly one marker type. Mediator is handed the "
                + "marker AND the module type; supply only one and the handlers are never "
                + "discovered, with no error until the first request needs them.");
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EveryModule_Should_HaveAMatchingContractsAssembly()
    {
        foreach (Assembly module in ArchitectureFixture.ModuleAssemblies)
        {
            string expected = module.GetName().Name + ".Contracts";

            ArchitectureFixture.ContractsAssemblies
                .Select(a => a.GetName().Name)
                .ShouldContain(
                    expected,
                    $"{module.GetName().Name} has no {expected}. Every module is a runtime plus "
                    + "Contracts pair, even when the public surface is small.");
        }
    }

    #endregion
}
