using System.Reflection;
using Dental.Framework.Eventing.Abstractions;
using FluentValidation;
using Shouldly;

namespace Dental.Architecture.Tests.Rules;

/// <summary>Naming and placement rules that keep a feature findable from its route.</summary>
public sealed class NamingConventionTests
{
    #region Happy Path

    [Fact]
    public void IntegrationEvents_Should_LiveInAContractsAssembly()
    {
        List<string> offending =
        [
            .. ArchitectureFixture.ModuleAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsAbstract: false } && t.IsAssignableTo(typeof(IIntegrationEvent)))
                .Select(t => t.FullName!),
        ];

        offending.ShouldBeEmpty(
            "An integration event is part of a module's public surface, so it belongs in the "
            + "Contracts assembly. One declared in the runtime cannot be handled by anyone else. "
            + "Offending: " + string.Join(", ", offending));
    }

    [Fact]
    public void IntegrationEvents_Should_BeNamedConsistently()
    {
        List<string> offending =
        [
            .. ArchitectureFixture.ContractsAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsAbstract: false } && t.IsAssignableTo(typeof(IIntegrationEvent)))
                .Where(t => !t.Name.EndsWith("IntegrationEvent", StringComparison.Ordinal))
                .Select(t => t.FullName!),
        ];

        offending.ShouldBeEmpty(
            "Integration events end in 'IntegrationEvent'. The name is stored in the outbox, so it "
            + "is also a wire contract - which is exactly why it should look like one. Offending: "
            + string.Join(", ", offending));
    }

    [Fact]
    public void IntegrationEventHandlers_Should_BeSealedAndInAnEventsNamespace()
    {
        List<string> offending =
        [
            .. ArchitectureFixture.ModuleAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType
                    && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
                .Where(t => !t.IsSealed
                            || t.Namespace?.EndsWith(".Events", StringComparison.Ordinal) != true)
                .Select(t => t.FullName!),
        ];

        offending.ShouldBeEmpty(
            "Integration event handlers are sealed and live in the module's Events namespace. "
            + "Offending: " + string.Join(", ", offending));
    }

    [Fact]
    public void Validators_Should_BeNamedAfterTheMessageTheyValidate()
    {
        List<string> offending = [];

        IEnumerable<Type> validators = ArchitectureFixture.ModuleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)));

        foreach (Type validator in validators)
        {
            Type message = validator.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                .GetGenericArguments()[0];

            if (!string.Equals(validator.Name, message.Name + "Validator", StringComparison.Ordinal))
            {
                offending.Add($"{validator.FullName} validates {message.Name}");
            }
        }

        offending.ShouldBeEmpty(
            "A validator is named {Message}Validator and sits in the same feature folder, so it is "
            + "obvious at a glance whether a message has one. Offending: "
            + string.Join(", ", offending));
    }

    [Fact]
    public void DbContexts_Should_BeSealed()
    {
        List<string> offending =
        [
            .. ArchitectureFixture.ModuleAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsAbstract: false }
                            && t.IsAssignableTo(typeof(Microsoft.EntityFrameworkCore.DbContext))
                            && !t.IsSealed)
                .Select(t => t.FullName!),
        ];

        offending.ShouldBeEmpty(
            "A module DbContext is sealed. Deriving from one is how OnModelCreating gets overridden "
            + "without the final base call, which silently drops every tenant filter. Offending: "
            + string.Join(", ", offending));
    }

    #endregion
}
