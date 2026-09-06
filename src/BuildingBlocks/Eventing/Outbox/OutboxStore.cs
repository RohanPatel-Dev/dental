using Dental.Framework.Core.Contracts;
using Dental.Framework.Eventing.Abstractions;
using Dental.Framework.Eventing.Serialization;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dental.Framework.Eventing.Outbox;

/// <summary>
/// Outbox writer bound to one module <c>DbContext</c>. Registered per context by
/// <c>AddEventingForDbContext&lt;TContext&gt;()</c>.
/// </summary>
/// <typeparam name="TContext">The module context that owns the outbox table.</typeparam>
/// <param name="context">The module context.</param>
/// <param name="tenantContextAccessor">Supplies the tenant stamped onto the row.</param>
/// <param name="requestContext">Supplies the correlation identifier.</param>
/// <param name="timeProvider">Clock.</param>
public sealed class OutboxStore<TContext>(
    TContext context,
    IMultiTenantContextAccessor tenantContextAccessor,
    IRequestContext requestContext,
    TimeProvider timeProvider) : IOutboxStore
    where TContext : DbContext
{
    /// <inheritdoc />
    public Task AddAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        string? tenantId = integrationEvent.TenantId
            ?? tenantContextAccessor.MultiTenantContext?.TenantInfo?.Id;

        OutboxMessage message = new()
        {
            EventType = IntegrationEventSerializer.GetTypeName(integrationEvent.GetType()),
            Payload = IntegrationEventSerializer.Serialize(integrationEvent),
            CorrelationId = integrationEvent.CorrelationId ?? requestContext.CorrelationId,
            OccurredOnUtc = integrationEvent.OccurredOnUtc,
            CreatedAt = timeProvider.GetUtcNow(),
            TenantId = tenantId ?? string.Empty,
        };

        context.Set<OutboxMessage>().Add(message);
        return Task.CompletedTask;
    }
}
