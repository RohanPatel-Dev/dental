using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dental.Framework.Jobs;

/// <summary>Wires Hangfire with Postgres storage and the DI job activator.</summary>
public static class JobsRegistration
{
    /// <summary>Registers the Hangfire client, server and <see cref="IJobService"/>.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddHeroJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<JobOptions>()
            .BindConfiguration(nameof(JobOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        JobOptions options = configuration.GetSection(nameof(JobOptions)).Get<JobOptions>()
            ?? new JobOptions();

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(postgres =>
                postgres.UseNpgsqlConnection(options.ConnectionString)));

        services.AddHangfireServer(server =>
        {
            server.Queues = [.. options.Queues];
            if (options.WorkerCount > 0)
            {
                server.WorkerCount = options.WorkerCount;
            }
        });

        services.TryAddSingleton<IJobService, HangfireJobService>();

        return services;
    }

    /// <summary>
    /// Registers a job service that throws on every call, for processes that must not enqueue work.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddNoOpJobs(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IJobService, NoOpJobService>();
        return services;
    }
}
