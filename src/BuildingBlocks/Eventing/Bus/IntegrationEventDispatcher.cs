using System.Reflection;
using Dental.Framework.Eventing.Abstractions;
using Dental.Framework.Eventing.Inbox;
using Dental.Framework.Core.Contracts;
using Dental.Framework.Eventing.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dental.Framework.Eventing.Bus;

/// <summary>
/// Resolves and invokes the handlers for one integration event in a fresh DI scope, restoring the
/// tenant context and consulting the inbox first.
/// </summary>
/// <param name="scopeFactory">Creates the per-event scope.</param>
/// <param name="logger">Logger.</param>
public sealed class IntegrationEventDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<IntegrationEventDispatcher> logger)
{
    /// <summary>
    /// Invokes every registered handler for the event, exactly once each.
    /// </summary>
    /// <param name="integrationEvent">The event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all handlers have run.</returns>
    /// <remarks>
    /// The scope has no HTTP context and no ambient tenant, so the tenant is restored from
    /// <see cref="IIntegrationEvent.TenantId"/> before any handler touches a filtered
    /// <c>DbContext</c>.
    /// </remarks>
    public async Task DispatchAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        Type eventType = integrationEvent.GetType();
        Type handlerInterface = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        string storedTypeName = IntegrationEventSerializer.GetTypeName(eventType);

        using IServiceScope scope = scopeFactory.CreateScope();

        // Resolved here and applied HERE, in this frame: an AsyncLocal written inside an awaited
        // helper does not flow back to its caller, so restoring the tenant "inside" a helper would
        // leave every handler below running with no tenant at all.
        TenantSnapshot? tenant = await ResolveTenantAsync(
                scope.ServiceProvider,
                integrationEvent.TenantId,
                cancellationToken)
            .ConfigureAwait(false);

        if (tenant is not null)
        {
            scope.ServiceProvider.GetRequiredService<ITenantContextRestorer>().Apply(tenant);

            logger.LogDebug(
                "Restored tenant {TenantId} for handlers of {EventType}.",
                tenant.Id,
                eventType.Name);
        }

        object[] handlers = [.. scope.ServiceProvider.GetServices(handlerInterface).OfType<object>()];
        if (handlers.Length == 0)
        {
            logger.LogDebug("No handler registered for {EventType}.", eventType.Name);
            return;
        }

        IInboxStore[] inboxStores = [.. scope.ServiceProvider.GetServices<IInboxStore>()];

        foreach (object handler in handlers)
        {
            await InvokeHandlerAsync(
                    handler,
                    handlerInterface,
                    integrationEvent,
                    storedTypeName,
                    inboxStores,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task InvokeHandlerAsync(
        object handler,
        Type handlerInterface,
        IIntegrationEvent integrationEvent,
        string storedTypeName,
        IReadOnlyCollection<IInboxStore> inboxStores,
        CancellationToken cancellationToken)
    {
        Type handlerType = handler.GetType();
        string handlerName = handlerType.FullName!;
        string handlerAssembly = handlerType.Assembly.GetName().Name!;

        IInboxStore? inbox = inboxStores.FirstOrDefault(
                s => string.Equals(s.OwnerAssembly, handlerAssembly, StringComparison.Ordinal))
            ?? inboxStores.FirstOrDefault();

        if (inbox is not null)
        {
            bool claimed = await inbox
                .TryClaimAsync(integrationEvent.Id, handlerName, storedTypeName, cancellationToken)
                .ConfigureAwait(false);

            if (!claimed)
            {
                logger.LogDebug(
                    "Skipping already-processed event {EventId} for handler {HandlerName}.",
                    integrationEvent.Id,
                    handlerName);
                return;
            }
        }

        MethodInfo method = handlerInterface.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!;
        Task task = (Task)method.Invoke(handler, [integrationEvent, cancellationToken])!;
        await task.ConfigureAwait(false);

        // The handler normally saves the claim alongside its own work. This covers a handler that
        // had nothing to write, so the claim would otherwise be staged and never persisted.
        if (inbox is not null)
        {
            await inbox.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogDebug(
            "Handled event {EventId} with {HandlerName}.",
            integrationEvent.Id,
            handlerName);
    }

    private async Task<TenantSnapshot?> ResolveTenantAsync(
        IServiceProvider provider,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        ITenantContextRestorer? restorer = provider.GetService<ITenantContextRestorer>();
        if (restorer is null)
        {
            logger.LogWarning(
                "No tenant context restorer is registered; tenant {TenantId} could not be restored "
                + "for the handler scope and tenant filtered queries will fail.",
                tenantId);
            return null;
        }

        return await restorer.ResolveAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }
}
