using Dental.Framework.Persistence.Contexts;
using Dental.Framework.Persistence.Interceptors;
using Dental.Framework.Persistence.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dental.Framework.Persistence.Extensions;

/// <summary>Registers a module <c>DbContext</c> with the shared interceptors and Npgsql setup.</summary>
public static class PersistenceRegistration
{
    /// <summary>
    /// Registers the shared persistence services. Called once per process by the platform; modules
    /// only call <see cref="AddHeroDbContext{TContext}"/>.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddHeroPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(nameof(DatabaseOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<AuditingInterceptor>();
        services.TryAddScoped<SoftDeleteInterceptor>();
        services.TryAddScoped<DomainEventDispatchInterceptor>();

        _ = configuration;
        return services;
    }

    /// <summary>
    /// Registers a module <c>DbContext</c> against the single connection string, wiring the audit,
    /// tenant, soft delete and domain event interceptors.
    /// </summary>
    /// <typeparam name="TContext">The module context type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// The migrations assembly comes from the single process-wide
    /// <c>DatabaseOptions:MigrationsAssembly</c>. A host that needs its own migrations project must
    /// therefore be its own process with its own migrator.
    /// </remarks>
    public static IServiceCollection AddHeroDbContext<TContext>(this IServiceCollection services)
        where TContext : BaseDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<TContext>((serviceProvider, builder) =>
        {
            DatabaseOptions options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;

            builder.UseNpgsql(options.ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(options.MigrationsAssembly);
                npgsql.CommandTimeout(options.CommandTimeoutSeconds);
                if (options.MaxRetryCount > 0)
                {
                    npgsql.EnableRetryOnFailure(options.MaxRetryCount);
                }
            });

            builder.EnableSensitiveDataLogging(options.EnableSensitiveDataLogging);
            builder.EnableDetailedErrors(options.EnableDetailedErrors);

            builder.AddInterceptors(
                serviceProvider.GetRequiredService<AuditingInterceptor>(),
                serviceProvider.GetRequiredService<SoftDeleteInterceptor>(),
                serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>());
        });

        return services;
    }
}
