using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Dental.Framework.Caching;

/// <summary>Wires <see cref="HybridCache"/> with an optional Redis L2 and OTel instrumentation.</summary>
public static class CachingRegistration
{
    /// <summary>
    /// Registers the cache. Inject <see cref="HybridCache"/> everywhere - never
    /// <c>IDistributedCache</c>, which loses stampede protection and tag invalidation.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// One shared <see cref="IConnectionMultiplexer"/> backs both the L2 cache and the DataProtection
    /// key ring, so a single Redis outage degrades both consistently instead of half the app.
    /// </remarks>
    public static IServiceCollection AddHeroCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CachingOptions>()
            .BindConfiguration(nameof(CachingOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        CachingOptions options = configuration.GetSection(nameof(CachingOptions)).Get<CachingOptions>()
            ?? new CachingOptions();

        if (!string.IsNullOrWhiteSpace(options.Redis))
        {
            string connectionString = options.Redis;

            // ONE multiplexer, connected lazily, shared by the L2 cache, the SignalR backplane and
            // the DataProtection key ring. Connecting eagerly here would make a cold Redis a
            // startup failure instead of a degraded cache.
            Lazy<IConnectionMultiplexer> multiplexer = new(
                () => ConnectionMultiplexer.Connect(connectionString),
                LazyThreadSafetyMode.ExecutionAndPublication);

            services.TryAddSingleton<IConnectionMultiplexer>(_ => multiplexer.Value);

            services.AddStackExchangeRedisCache(redis =>
            {
                redis.InstanceName = options.InstanceName;
                redis.ConnectionMultiplexerFactory = () => Task.FromResult(multiplexer.Value);
            });
        }

        services.AddHybridCache(hybrid =>
        {
            hybrid.MaximumPayloadBytes = options.MaximumPayloadBytes;
            hybrid.MaximumKeyLength = options.MaximumKeyLength;
            hybrid.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(options.DefaultExpirationSeconds),
                LocalCacheExpiration = TimeSpan.FromSeconds(options.LocalExpirationSeconds),
            };
        });

        DecorateHybridCache(services);

        return services;
    }

    /// <summary>
    /// Minimal decorator support so the framework does not pull in a DI extension package for one
    /// registration.
    /// </summary>
    /// <typeparam name="TService">Service being decorated.</typeparam>
    /// <typeparam name="TDecorator">Decorator implementation taking the inner service.</typeparam>
    /// <param name="services">Service collection.</param>
    private static void Decorate<TService, TDecorator>(IServiceCollection services)
        where TService : class
        where TDecorator : class, TService
    {
        ServiceDescriptor? existing = services.LastOrDefault(d => d.ServiceType == typeof(TService))
            ?? throw new InvalidOperationException(
                $"Cannot decorate {typeof(TService).Name}: it is not registered.");

        services.Remove(existing);

        services.Add(new ServiceDescriptor(
            typeof(TService),
            provider =>
            {
                TService inner = (TService)CreateInstance(provider, existing);
                return ActivatorUtilities.CreateInstance<TDecorator>(provider, inner);
            },
            existing.Lifetime));
    }

    private static void DecorateHybridCache(IServiceCollection services) =>
        Decorate<HybridCache, InstrumentedHybridCache>(services);

    private static object CreateInstance(IServiceProvider provider, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is not null)
        {
            return descriptor.ImplementationInstance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return descriptor.ImplementationFactory(provider);
        }

        return ActivatorUtilities.GetServiceOrCreateInstance(provider, descriptor.ImplementationType!);
    }
}
