using System.Reflection;
using Shouldly;

namespace Dental.Architecture.Tests.Rules;

/// <summary>
/// The rule the whole architecture rests on: a module may reach another module only through its
/// <c>.Contracts</c> assembly.
/// </summary>
public sealed class ModuleBoundaryTests
{
    #region Happy Path

    [Fact]
    public void Modules_Should_NotReferenceAnotherModulesRuntimeAssembly()
    {
        foreach (Assembly module in ArchitectureFixture.ModuleAssemblies)
        {
            string name = module.GetName().Name!;

            string[] offending =
            [
                .. ArchitectureFixture.ReferencedAssemblyNames(module)
                    .Where(r => ArchitectureFixture.ModuleAssemblyNames.Contains(r, StringComparer.Ordinal))
                    .Where(r => !string.Equals(r, name, StringComparison.Ordinal)),
            ];

            offending.ShouldBeEmpty(
                $"{name} references another module's RUNTIME assembly ({string.Join(", ", offending)}). "
                + "Reach other modules only through their .Contracts project, or invert the "
                + "dependency with a contributor interface.");
        }
    }

    [Fact]
    public void ContractsAssemblies_Should_StayDependencyLight()
    {
        // A Contracts project is referenced by every consumer of the module, so anything it drags in
        // becomes a dependency of them all. EF Core, ASP.NET and the module runtime are the ones
        // that actually get pulled in by accident.
        string[] forbiddenPrefixes =
        [
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Dental.Framework.Persistence",
            "Dental.Framework.Web",
            "FluentValidation",
            "Hangfire.Core",
        ];

        // Matched exactly, not by prefix: Eventing.Abstractions is one of the three references a
        // Contracts project IS allowed, and a prefix rule would forbid it along with the runtime.
        string[] forbiddenExact = ["Dental.Framework.Eventing"];

        foreach (Assembly contracts in ArchitectureFixture.ContractsAssemblies)
        {
            IReadOnlyList<string> references = ArchitectureFixture.ReferencedAssemblyNames(contracts);

            string[] offending =
            [
                .. references.Where(r =>
                    forbiddenPrefixes.Any(f => r.StartsWith(f, StringComparison.Ordinal))
                    || forbiddenExact.Contains(r, StringComparer.Ordinal)),
            ];

            offending.ShouldBeEmpty(
                $"{contracts.GetName().Name} references {string.Join(", ", offending)}. "
                + "A Contracts project may reference only Framework.Shared, "
                + "Framework.Eventing.Abstractions and Mediator.Abstractions.");
        }
    }

    [Fact]
    public void ContractsAssemblies_Should_NotReferenceAnyModuleRuntime()
    {
        foreach (Assembly contracts in ArchitectureFixture.ContractsAssemblies)
        {
            string[] offending =
            [
                .. ArchitectureFixture.ReferencedAssemblyNames(contracts)
                    .Where(r => ArchitectureFixture.ModuleAssemblyNames.Contains(r, StringComparer.Ordinal)),
            ];

            offending.ShouldBeEmpty(
                $"{contracts.GetName().Name} references a module runtime ({string.Join(", ", offending)}), "
                + "which would make the dependency graph circular.");
        }
    }

    [Fact]
    public void BuildingBlocks_Should_NeverReferenceAModule()
    {
        foreach (Assembly framework in ArchitectureFixture.FrameworkAssemblies.Distinct())
        {
            string[] offending =
            [
                .. ArchitectureFixture.ReferencedAssemblyNames(framework)
                    .Where(r => r.StartsWith("Dental.Modules", StringComparison.Ordinal)),
            ];

            offending.ShouldBeEmpty(
                $"{framework.GetName().Name} references {string.Join(", ", offending)}. "
                + "BuildingBlocks is the bottom of the graph: if it needs something from a module, "
                + "the module must implement an interface the framework owns.");
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Modules_Should_NotDependOnEachOthersContracts_Circularly()
    {
        Dictionary<string, string[]> contractsReferences = ArchitectureFixture.ModuleAssemblies
            .ToDictionary(
                m => m.GetName().Name!,
                m =>
                    (string[])[
                        .. ArchitectureFixture.ReferencedAssemblyNames(m)
                            .Where(r => r.StartsWith("Dental.Modules", StringComparison.Ordinal)
                                        && r.EndsWith(".Contracts", StringComparison.Ordinal))
                            .Select(r => r[..^".Contracts".Length]),
                    ],
                StringComparer.Ordinal);

        foreach ((string module, string[] dependencies) in contractsReferences)
        {
            // Every module references its OWN contracts; only a mutual reference is a cycle.
            foreach (string dependency in dependencies.Where(d =>
                         !string.Equals(d, module, StringComparison.Ordinal)))
            {
                if (!contractsReferences.TryGetValue(dependency, out string[]? reverse))
                {
                    continue;
                }

                reverse.ShouldNotContain(
                    module,
                    $"{module} and {dependency} reference each other's contracts. "
                    + "Break the cycle with an integration event or a contributor interface.");
            }
        }
    }

    #endregion
}
