# Extract a module into its own host

Notifications is the worked example: read it alongside this.

## Before you start

Ask whether the module still needs another module's data synchronously. If it does, extraction means
building a local read model — that is the real cost, not the plumbing.

## 1. The host

`src/Host/Dental.{Name}Host/Program.cs`, mirroring `Dental.Api`:

* Mediator marker pair and module assemblies — **registration sites 5 and 6**;
* `AddHeroPlatform` with the features this host actually serves. Notifications registers the
  realtime services but sets `MapRealtime = false`: it pushes through the shared backplane and
  serves no hub of its own;
* `AddIntegrationEventTypeRegistry(..., "dental-{name}")` with every event type it consumes. An
  unregistered type is parked, not delivered.

## 2. The migrator and migrations

`Dental.{Name}Migrator` (sites 7 and 8) and `Dental.{Name}.Migrations.PostgreSQL`. A separate
project because `AddHeroDbContext` resolves one process-wide `MigrationsAssembly`.

## 3. The read model

Anything the module used to ask another module for in-process now arrives as an event. Notifications
keeps `PatientContact`, projected from `PatientRegistered`, `PatientContactChanged`,
`PatientConsentChanged` and the erasure event. Enrich the publisher's events rather than adding a
callback — a callback is a distributed monolith.

## 4. Eventing across processes

Switch `EventingOptions:Provider` to `RabbitMq` for both hosts. In-memory eventing only reaches
handlers in the same process, so an extracted host receives nothing until the bus is real.

## 5. Aspire

Add the host, its migrator (`WaitForCompletion`) and its own Hangfire database to `AppHost.cs`. Give
it its own logical database for jobs: two hosts sharing one Hangfire schema fight over the same
queues.

## 6. Verify

* Both hosts start.
* An event published by the API reaches the extracted host — watch the outbox row disappear and the
  inbox row appear.
* The extracted host resolves nothing from the modules it no longer loads.
