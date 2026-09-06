namespace Dental.Framework.Eventing.Abstractions;

/// <summary>
/// Convenience base for integration events. Derive with a positional record and add your payload:
/// <c>public sealed record AppointmentBookedIntegrationEvent(Guid AppointmentId) : IntegrationEvent;</c>.
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string? TenantId { get; init; }

    /// <inheritdoc />
    public string? CorrelationId { get; init; }

    /// <inheritdoc />
    public string Source { get; init; } = string.Empty;
}
