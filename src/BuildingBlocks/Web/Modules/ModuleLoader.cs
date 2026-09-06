using System.Collections.Immutable;
using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dental.Framework.Web.Modules;

/// <summary>
/// Discovers, orders and drives the three lifecycle phases of every module in the process.
/// </summary>
/// <remarks>
/// The loaded set is held as an immutable array swapped atomically - never a mutable static list
/// enumerated while another thread appends to it.
/// </remarks>
public static class ModuleLoader
{
    private static ImmutableArray<IModule> _modules = [];

    /// <summary>Modules loaded in this process, in load order.</summary>
    public static ImmutableArray<IModule> LoadedModules => _modules;

    /// <summary>
    /// Scans the assemblies for <see cref="FshModuleAttribute"/>, instantiates each module in order
    /// and runs phase 1. Also auto-registers every FluentValidation validator found.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="assemblies">Module assemblies to scan.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <remarks>
    /// Forgetting to add an assembly here is one of the four registration sites. The module simply
    /// never loads and no endpoint appears - there is no error.
    /// </remarks>
    public static IHostApplicationBuilder AddModules(
        this IHostApplicationBuilder builder,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assemblies);

        IModule[] modules =
        [
            .. assemblies
                .SelectMany(a => a.GetCustomAttributes<FshModuleAttribute>())
                .OrderBy(a => a.Order)
                .ThenBy(a => a.ModuleType.Name, StringComparer.Ordinal)
                .Select(Instantiate),
        ];

        foreach (IModule module in modules)
        {
            module.ConfigureServices(builder);
        }

        builder.Services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true);

        ImmutableInterlocked.InterlockedExchange(ref _modules, [.. modules]);

        return builder;
    }

    /// <summary>Runs phase 2 for every loaded module.</summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder, for chaining.</returns>
    public static IApplicationBuilder UseModuleMiddlewares(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        foreach (IModule module in _modules)
        {
            module.ConfigureMiddleware(app);
        }

        return app;
    }

    /// <summary>Runs phase 3 for every loaded module.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapModules(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        foreach (IModule module in _modules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }

    /// <summary>Clears the loaded set. Test only.</summary>
    public static void Reset() => ImmutableInterlocked.InterlockedExchange(ref _modules, []);

    private static IModule Instantiate(FshModuleAttribute attribute)
    {
        if (!attribute.ModuleType.IsAssignableTo(typeof(IModule)))
        {
            throw new InvalidOperationException(
                $"[FshModule] names '{attribute.ModuleType.FullName}', which does not implement "
                + $"{nameof(IModule)}.");
        }

        return (IModule)Activator.CreateInstance(attribute.ModuleType)!;
    }
}
