# Architecture rules

## Modules

* A module is a pair: `Modules.{Name}` (runtime) and `Modules.{Name}.Contracts` (public surface).
* A module may reference **another module's Contracts project**. It may never reference another
  module's runtime project. `ModuleBoundaryTests` fails the build if it does.
* Contracts stay dependency-light: Mediator abstractions, the eventing abstractions, and
  `Dental.Framework.Shared`. Never EF Core, never ASP.NET. Anything a Contracts project needs from
  the framework belongs in `Shared`, not in `Persistence` or `Web`.
* A module declares itself with `[assembly: FshModule(...)]` and one `IModule` implementation.
  `Order` decides registration and migration order: foundations (Tenancy 100, Identity 200,
  Auditing 300) before business modules (900+).

## Slices

* One folder per feature under `Features/v{n}/{Area}/{Action}/`: endpoint, command or query,
  handler, validator. If you are opening four files in four directories to change one behaviour,
  the slice is wrong.
* The command/query and its DTOs live in Contracts. The handler, the validator and the endpoint live
  in the runtime project.
* No repository layer. Handlers use the module's `DbContext` directly; a `Specification<T>` exists
  for the queries that genuinely repeat.

## Talking to another module

In order of preference:

1. **An integration event.** The publisher does not know who listens. This is the default.
2. **A contract service** (`IPatientService`) exposed from Contracts, when the caller needs an
   answer now. Keep these small and read-only where possible.
3. **A contributor interface** (`IChartContributor`) when a module wants to add to another module's
   view without either owning the other.

Never a direct `DbContext` reference across a module, and never a query that joins two modules'
tables.

## Extracting a module into its own host

The point of the design. A module that is genuinely independent needs, at the new host:

* its own `Program.cs` with all four registration sites,
* its own migrations project and migrator,
* a local read model for anything it used to ask another module for in-process. Notifications keeps
  `PatientContact`, fed by enriched events, precisely because `IPatientService` is not reachable
  from another process.

See `.agents/workflows/extract-a-module.md`.
