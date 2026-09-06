using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Dental.Framework.Quota;

/// <summary>Registers the quota subsystem.</summary>
public static class QuotaRegistration
{
    /// <summary>
    /// Registers <see cref="IQuotaService"/>, using Redis when a multiplexer is already registered
    /// and falling back to the in-memory implementation otherwise.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddHeroQuotas(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<QuotaOptions>()
            .BindConfiguration(nameof(QuotaOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IQuotaLimitProvider, NoQuotaLimitProvider>();

        bool hasRedis = services.Any(d => d.ServiceType == typeof(IConnectionMultiplexer));

        if (hasRedis)
        {
            services.TryAddSingleton<IQuotaService, RedisQuotaService>();
        }
        else
        {
            services.TryAddSingleton<IQuotaService, InMemoryQuotaService>();
        }

        return services;
    }
}
