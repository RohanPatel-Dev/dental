using System.ComponentModel.DataAnnotations;

namespace Dental.Framework.Eventing;

/// <summary>Eventing configuration, bound from the <c>EventingOptions</c> section.</summary>
/// <remarks>
/// <see cref="Provider"/> is read EAGERLY at registration time, before any test configuration
/// overlay applies. A test factory must re-register <c>IEventBus</c> in <c>ConfigureServices</c>
/// rather than merely overriding configuration.
/// </remarks>
public sealed class EventingOptions
{
    /// <summary>
    /// <c>InMemory</c> for synchronous in-process dispatch, <c>RabbitMQ</c> for durable cross-process
    /// delivery. The in-memory bus cannot cross a process boundary, so any extracted host needs RabbitMQ.
    /// </summary>
    public string Provider { get; set; } = EventingProviders.InMemory;

    /// <summary>RabbitMQ connection string, required when <see cref="Provider"/> is RabbitMQ.</summary>
    public string? RabbitMq { get; set; }

    /// <summary>Topic exchange integration events are published to.</summary>
    public string ExchangeName { get; set; } = "dental.events";

    /// <summary>Seconds between outbox dispatch sweeps.</summary>
    [Range(1, 3600)]
    public int OutboxDispatchIntervalSeconds { get; set; } = 10;

    /// <summary>Messages claimed per sweep.</summary>
    [Range(1, 1000)]
    public int OutboxBatchSize { get; set; } = 100;

    /// <summary>Publish attempts before a message is dead lettered.</summary>
    [Range(1, 20)]
    public int OutboxMaxRetries { get; set; } = 5;

    /// <summary>Consumer side redeliveries before a message goes to the per-queue dead letter queue.</summary>
    [Range(1, 20)]
    public int ConsumerMaxRetries { get; set; } = 3;

    /// <summary>Days a processed inbox row is kept before it is swept.</summary>
    [Range(1, 365)]
    public int InboxRetentionDays { get; set; } = 14;
}

/// <summary>Valid <see cref="EventingOptions.Provider"/> values.</summary>
public static class EventingProviders
{
    /// <summary>Synchronous in-process dispatch. Single process only.</summary>
    public const string InMemory = "InMemory";

    /// <summary>Durable topic exchange. Required for any cross-host eventing.</summary>
    public const string RabbitMq = "RabbitMQ";
}
