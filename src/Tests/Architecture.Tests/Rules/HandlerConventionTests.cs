using System.Reflection;
using Dental.Framework.Shared.Pagination;
using FluentValidation;
using Mediator;
using Shouldly;

namespace Dental.Architecture.Tests.Rules;

/// <summary>Conventions that make handlers predictable and keep unvalidated input out of them.</summary>
public sealed class HandlerConventionTests
{
    private static readonly Type[] HandlerInterfaces =
        [typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

    #region Happy Path

    [Fact]
    public void EveryCommandHandler_Should_HaveAMatchingValidator()
    {
        Type[] validators =
        [
            .. ArchitectureFixture.ModuleAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsAbstract: false, IsInterface: false })
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))),
        ];

        HashSet<Type> validatedMessages =
        [
            .. validators
                .SelectMany(v => v.GetInterfaces())
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                .Select(i => i.GetGenericArguments()[0]),
        ];

        List<string> missing = [];

        foreach (Type message in CommandMessages().Concat(PaginatedQueryMessages()).Distinct())
        {
            if (!validatedMessages.Contains(message))
            {
                missing.Add(message.FullName!);
            }
        }

        missing.ShouldBeEmpty(
            "Every command and every paginated query needs a validator, so a handler never has to "
            + "defend against a shape a rule already rejects, and no query can ask for an unbounded "
            + "page. Missing: " + string.Join(", ", missing));
    }

    [Fact]
    public void Handlers_Should_BePublicAndSealed()
    {
        List<string> offending =
        [
            .. HandlerTypes()
                .Where(t => !t.IsSealed || !t.IsPublic)
                .Select(t => t.FullName!),
        ];

        offending.ShouldBeEmpty(
            "Handlers are public sealed: sealed because nothing should derive from one, public "
            + "because the Mediator source generator has to see them. Offending: "
            + string.Join(", ", offending));
    }

    [Fact]
    public void Handlers_Should_LiveInAFeaturesFolder()
    {
        List<string> offending =
        [
            .. HandlerTypes()
                .Where(t => t.Namespace?.Contains(".Features.", StringComparison.Ordinal) != true)
                .Select(t => t.FullName!),
        ];

        offending.ShouldBeEmpty(
            "A handler belongs in Features/v{n}/{Area}/{Feature}, beside its validator and endpoint. "
            + "Offending: " + string.Join(", ", offending));
    }

    [Fact]
    public void Handlers_Should_ReturnValueTask()
    {
        List<string> offending = [];

        foreach (Type handler in HandlerTypes())
        {
            foreach (MethodInfo method in handler.GetMethods()
                         .Where(m => m.Name == "Handle" && m.DeclaringType == handler))
            {
                if (!method.ReturnType.IsGenericType
                    || method.ReturnType.GetGenericTypeDefinition() != typeof(ValueTask<>))
                {
                    offending.Add($"{handler.FullName}.{method.Name}");
                }
            }
        }

        offending.ShouldBeEmpty(
            "Handlers return ValueTask<T>: most complete synchronously, and Task would allocate for "
            + "every one of them. Offending: " + string.Join(", ", offending));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void PaginatedQueries_Should_ReturnPagedResponse()
    {
        List<string> offending = [];

        foreach (Type message in PaginatedQueryMessages())
        {
            Type? queryInterface = message.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType
                                     && i.GetGenericTypeDefinition() == typeof(IQuery<>));

            Type? response = queryInterface?.GetGenericArguments()[0];

            if (response is null
                || !response.IsGenericType
                || response.GetGenericTypeDefinition() != typeof(PagedResponse<>))
            {
                offending.Add(message.FullName!);
            }
        }

        offending.ShouldBeEmpty(
            "An IPagedQuery must return PagedResponse<T>, so every caller gets the same envelope. "
            + "Offending: " + string.Join(", ", offending));
    }

    #endregion

    private static IEnumerable<Type> HandlerTypes() =>
        ArchitectureFixture.ModuleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType
                && HandlerInterfaces.Contains(i.GetGenericTypeDefinition())));

    private static IEnumerable<Type> CommandMessages() =>
        ArchitectureFixture.ContractsAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)));

    private static IEnumerable<Type> PaginatedQueryMessages() =>
        ArchitectureFixture.ContractsAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.IsAssignableTo(typeof(IPagedQuery)));
}
