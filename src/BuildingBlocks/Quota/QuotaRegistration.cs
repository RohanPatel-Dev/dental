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
    /// <param name="enforce">
    /// When false, an unlimited implementation is registered instead. The abstraction is always
    /// present, because feature code depends on it in every host.
    /// </param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddHeroQuotas(
        this IServiceCollection services,
        IConfiguration configuration,
        bool enforce = true)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<QuotaOptions>()
            .BindConfiguration(nameof(QuotaOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IQuotaLimitProvider, NoQuotaLimitProvider>();

        if (!enforce)
        {
            services.TryAddSingleton<IQuotaService, UnlimitedQuotaService>();
            return services;
        }

        bool hasRedis = services.Any(d => d.ServiceType == typeof(IConnectionMultiplexer));

        // SCOPED, not singleton: the limit provider reads the tenant's plan through a scoped module
        // service, and a singleton cannot consume one. Anything that must outlive the request -
        // the in-memory counters - lives in its own singleton instead.
        if (hasRedis)
        {
            services.TryAddScoped<IQuotaService, RedisQuotaService>();
        }
        else
        {
            services.TryAddSingleton<InMemoryQuotaCounterStore>();
            services.TryAddScoped<IQuotaService, InMemoryQuotaService>();
        }

        return services;
    }
}
