---
name: add-an-integration-event
description: Publish a fact across a module or process boundary through the outbox, and handle it exactly once.
---

# Add an integration event

## 1. Declare it in the publisher's Contracts

```csharp
public sealed record AppointmentCancelledIntegrationEvent(
    Guid AppointmentId,
    Guid PatientId,
    DateTimeOffset StartsAt,
    string Reason) : IntegrationEvent;
```

Carry everything a subscriber needs. An event that forces the subscriber to call back into the
publisher stops working the moment the subscriber moves to its own process — that is why
`PatientRegisteredIntegrationEvent` carries the contact details.

**The stored type name is `Namespace.Type, Assembly`. Renaming or moving this type orphans every
queued row.** Add a new type instead.

## 2. Publish it through the outbox

In the handler, on the same `SaveChanges` as the change it describes:

```csharp
await outbox.StageAsync(new AppointmentCancelledIntegrationEvent(...), cancellationToken);
await context.SaveChangesAsync(cancellationToken);
```

`IOutboxStore<TContext>` is generic on purpose — resolved without its context type, a handler writes
into another module's change tracker and the row vanishes. Never publish straight onto `IEventBus`.

## 3. Handle it in the subscriber

```csharp
public sealed class AppointmentCancelledHandler(
    ITenantContextRestorer tenants,
    NotificationsDbContext context)
    : IIntegrationEventHandler<AppointmentCancelledIntegrationEvent>
{
    public async Task HandleAsync(AppointmentCancelledIntegrationEvent e, CancellationToken ct = default)
    {
        TenantSnapshot? snapshot = await tenants.ResolveAsync(e.TenantId!, ct);
        if (snapshot is null) { return; }

        tenants.Apply(snapshot);   // IN THIS FRAME: an AsyncLocal set inside an awaited
                                   // helper does not flow back to the caller.
        ...
        await context.SaveChangesAsync(ct);
    }
}
```

The handler runs in a fresh scope with no HTTP context and no ambient tenant. Deduplication is the
inbox's, keyed `(EventId, HandlerName)` — do not hand-roll it. The claim commits with your
`SaveChanges`; if you write nothing, call `CommitAsync` so the claim is still recorded.

## 4. Register the type where it is consumed

An extracted host resolves events by name, so add the type to its
`AddIntegrationEventTypeRegistry` list. Without it the row is parked, not delivered.

## 5. Verify

The full path — command, outbox, dispatcher, restored tenant, handler — is covered by
`TenantProvisioningTests` in the integration suite. Run it against a real Postgres, not a stub.
