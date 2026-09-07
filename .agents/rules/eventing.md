# Eventing rules

Two tiers, and they are not interchangeable.

## Domain events — inside one module, inside one transaction

* Derive from `DomainEvent` (which is a Mediator `INotification`).
* Raised by an aggregate, dispatched by `DomainEventDispatchInterceptor` as part of `SaveChanges`.
* **Every domain event must have a handler.** The Mediator source generator fails the build with
  `MSG0005` if one has none — which is the right answer: an event nobody handles is a lie about the
  design. If the only interested party is another module, it is an integration event, not a domain
  event.

## Integration events — across a module or process boundary

* Derive from `IntegrationEvent`, live in the publisher's Contracts project.
* **Always published through the outbox**, never straight onto the bus. `IOutboxStore<TContext>` is
  generic on purpose: resolved without its context type, the last registration wins and a handler
  writes into a different module's change tracker.
* Handlers implement `IIntegrationEventHandler<TEvent>`, are sealed, and live in `Events/`.
* A handler runs in a fresh scope with no HTTP context and no ambient tenant. Restore the tenant
  from `IIntegrationEvent.TenantId` through `ITenantContextRestorer`: `ResolveAsync` then `Apply`,
  and `Apply` **in the calling frame** — an AsyncLocal written inside an awaited helper does not
  flow back out.
* Deduplication is the inbox's job, keyed `(EventId, HandlerName)`. Do not hand-roll it. The claim
  is staged and committed with the handler's own `SaveChanges`, so a handler that throws does not
  lose its event.
* The stored type name is `Namespace.Type, Assembly`. **Renaming or moving an event orphans every
  queued row.** Introduce a new type instead.

## Choosing

Ask who needs to know. Inside the aggregate's own module, in the same transaction → domain event.
Anyone else, ever, including a future extracted host → integration event.
