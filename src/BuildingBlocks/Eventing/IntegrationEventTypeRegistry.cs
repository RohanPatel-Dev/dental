using System.Collections.Immutable;
using System.Reflection;
using Dental.Framework.Eventing.Abstractions;

namespace Dental.Framework.Eventing;

/// <summary>
/// Every integration event type this process knows about: those it handles, plus those it only
/// publishes and declared with an <see cref="IntegrationEventTypeMarker"/>.
/// </summary>
/// <remarks>
/// An event with no in-process handler and no marker gets no RabbitMQ queue, so it is published
/// into the void. This registry is the reason the marker exists.
/// </remarks>
public sealed class IntegrationEventTypeRegistry
{
    /// <summary>Creates the registry from the handlers and markers registered in DI.</summary>
    /// <param name="handlerTypes">Concrete handler types discovered at registration.</param>
    /// <param name="markers">Explicit markers for events this process only publishes.</param>
    /// <param name="consumerName">Prefix for queue names, normally the host assembly name.</param>
    public IntegrationEventTypeRegistry(
        IEnumerable<Type> handlerTypes,
        IEnumerable<IntegrationEventTypeMarker> markers,
        string consumerName)
    {
        ArgumentNullException.ThrowIfNull(handlerTypes);
        ArgumentNullException.ThrowIfNull(markers);

        ConsumerName = consumerName;

        IEnumerable<Type> fromHandlers = handlerTypes
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType
                        && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
            .Select(i => i.GetGenericArguments()[0]);

        EventTypes = [.. fromHandlers.Concat(markers.Select(m => m.EventType)).Distinct()];
    }

    /// <summary>Prefix applied to every queue this process declares.</summary>
    public string ConsumerName { get; }

    /// <summary>Distinct event types this process should bind a queue for.</summary>
    public ImmutableArray<Type> EventTypes { get; }

    /// <summary>Finds every concrete integration event handler in the given assemblies.</summary>
    /// <param name="assemblies">Assemblies to scan.</param>
    /// <returns>The handler types.</returns>
    public static IEnumerable<Type> DiscoverHandlers(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.GetInterfaces().Any(i =>
                            i.IsGenericType
                            && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)));
    }
}
