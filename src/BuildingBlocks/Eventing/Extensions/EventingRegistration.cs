using System.Reflection;
using Dental.Framework.Eventing.Abstractions;
using Dental.Framework.Eventing.Bus;
using Dental.Framework.Eventing.Hosting;
using Dental.Framework.Eventing.Inbox;
using Dental.Framework.Eventing.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dental.Framework.Eventing.Extensions;

/// <summary>
/// The three eventing registration calls a module makes: core services, per-context stores, and
/// handler discovery.
/// </summary>
public static class EventingRegistration
{
    /// <summary>
    /// Registers the bus, the dispatcher and the hosted services. Safe to call once per module -
    /// subsequent calls are no-ops.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// <c>EventingOptions:Provider</c> is read EAGERLY here, before any later configuration overlay
    /// applies. A test factory must therefore remove and re-register <see cref="IEventBus"/> rather
    /// than only overriding configuration.
    /// </remarks>
    public static IServiceCollection AddEventingCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (services.Any(d => d.ServiceType == typeof(IntegrationEventDispatcher)))
        {
            return services;
        }

        services.AddOptions<EventingOptions>()
            .BindConfiguration(nameof(EventingOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IntegrationEventDispatcher>();

        EventingOptions options = configuration.GetSection(nameof(EventingOptions)).Get<EventingOptions>()
            ?? new EventingOptions();

        if (string.Equals(options.Provider, EventingProviders.RabbitMq, StringComparison.OrdinalIgnoreCase))
        {
            services.TryAddSingleton<RabbitMqConnectionProvider>();
            services.TryAddSingleton<IEventBus, RabbitMqEventBus>();
            services.AddHostedService<RabbitMqConsumerHostedService>();
        }
        else
        {
            services.TryAddSingleton<IEventBus, InMemoryEventBus>();
        }

        services.AddHostedService<OutboxDispatcherHostedService>();

        return services;
    }

    /// <summary>
    /// Registers the outbox writer, the outbox sweeper and the inbox for one module
    /// <c>DbContext</c>.
    /// </summary>
    /// <typeparam name="TContext">The module context.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddEventingForDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IOutboxStore, OutboxStore<TContext>>();
        services.AddScoped<IInboxStore, InboxStore<TContext>>();
        services.AddScoped<IOutboxSweeper, OutboxSweeper<TContext>>();

        return services;
    }

    /// <summary>
    /// Registers every <see cref="IIntegrationEventHandler{TEvent}"/> found in the given assemblies.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="assemblies">Assemblies to scan, normally just the module assembly.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddIntegrationEventHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (Type handlerType in IntegrationEventTypeRegistry.DiscoverHandlers(assemblies))
        {
            IEnumerable<Type> interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType
                            && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));

            foreach (Type handlerInterface in interfaces)
            {
                services.AddScoped(handlerInterface, handlerType);
            }
        }

        return services;
    }

    /// <summary>
    /// Builds the queue registry from everything registered so far. Called once by the platform,
    /// after all modules have registered their handlers and markers.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="assemblies">Module assemblies to scan for handlers.</param>
    /// <param name="consumerName">Queue name prefix, normally the host assembly name.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddIntegrationEventTypeRegistry(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        string consumerName)
    {
        ArgumentNullException.ThrowIfNull(services);

        Type[] handlerTypes = [.. IntegrationEventTypeRegistry.DiscoverHandlers(assemblies)];

        services.TryAddSingleton(provider => new IntegrationEventTypeRegistry(
            handlerTypes,
            provider.GetServices<IntegrationEventTypeMarker>(),
            consumerName));

        return services;
    }

    /// <summary>
    /// Declares an event this process publishes but does not handle, so the consumer still binds a
    /// queue for its routing key.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="services">Service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddIntegrationEventType<TEvent>(this IServiceCollection services)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(new IntegrationEventTypeMarker(typeof(TEvent)));
        return services;
    }
}
