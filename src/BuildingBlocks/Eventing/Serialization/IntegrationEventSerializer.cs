using System.Text.Json;
using System.Text.Json.Serialization;
using Dental.Framework.Eventing.Abstractions;

namespace Dental.Framework.Eventing.Serialization;

/// <summary>
/// Serializes integration events to and from the outbox payload column and the wire.
/// </summary>
/// <remarks>
/// The type name written is <see cref="Type.AssemblyQualifiedName"/> trimmed to
/// <c>Namespace.Type, Assembly</c>, so an assembly version bump does not orphan queued rows -
/// but a rename or a namespace move still does.
/// </remarks>
public static class IntegrationEventSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Writes the stable type name stored alongside the payload.</summary>
    /// <param name="eventType">The concrete event type.</param>
    /// <returns>A name of the form <c>Namespace.Type, Assembly</c>.</returns>
    public static string GetTypeName(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return $"{eventType.FullName}, {eventType.Assembly.GetName().Name}";
    }

    /// <summary>Serializes an event to JSON.</summary>
    /// <param name="integrationEvent">The event.</param>
    /// <returns>The JSON payload.</returns>
    public static string Serialize(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), Options);
    }

    /// <summary>Resolves a stored type name back to a runtime type.</summary>
    /// <param name="typeName">Name produced by <see cref="GetTypeName"/>.</param>
    /// <returns>The type, or <see langword="null"/> when it no longer exists.</returns>
    public static Type? ResolveType(string typeName) => Type.GetType(typeName, throwOnError: false);

    /// <summary>Deserializes a payload against a resolved type.</summary>
    /// <param name="payload">JSON payload.</param>
    /// <param name="eventType">Resolved event type.</param>
    /// <returns>The event instance.</returns>
    /// <exception cref="InvalidOperationException">The payload did not produce an event.</exception>
    public static IIntegrationEvent Deserialize(string payload, Type eventType)
    {
        object? value = JsonSerializer.Deserialize(payload, eventType, Options);
        return value as IIntegrationEvent
            ?? throw new InvalidOperationException(
                $"Payload did not deserialize into an {nameof(IIntegrationEvent)} of type '{eventType}'.");
    }
}
