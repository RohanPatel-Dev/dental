# AGENTS.md

The single source of truth for anyone — human or agent — working in this repository. `CLAUDE.md`
and `GEMINI.md` are one-line bridges to this file; the detail lives in `.agents/`.

## What this is

A dental practice-management system built as a **modular monolith with vertical slices**. One
composition-root Web API host loads every module; a module can also be lifted into its own host
without changing its code (Notifications already is). Multi-tenant by default: every request
resolves a tenant, and every tenant-owned row is filtered on it.

There is no Clean-Architecture layering. A feature lives in one folder — endpoint, command or
query, handler, validator — and you change it in one place.

## Layout

```
src/
  BuildingBlocks/          11 framework projects (Dental.Framework.*)
  Modules/{Name}/
    Modules.{Name}/          runtime: domain, data, features, events, services
    Modules.{Name}.Contracts/ the module's public surface: commands, queries, DTOs, events
  Host/
    Dental.Api                  the composition root
    Dental.DbMigrator           applies migrations and seeds; never the API
    Dental.NotificationsHost    the extracted module's own host
    Dental.NotificationsMigrator
    Dental.Migrations.PostgreSQL              migrations for the API host
    Dental.Notifications.Migrations.PostgreSQL migrations for the extracted host
    Dental.AppHost              .NET Aspire orchestration
  Tests/                    see src/Tests/README.md
clients/
  admin      operator console (Vite, port 5173)
  dashboard  practice app     (Vite, port 5174)
```

A module references **other modules' Contracts projects only**, never their runtime project. The
architecture tests enforce this; see `.agents/rules/architecture.md`.

## The eight registration sites

Adding a module means registering it in **four** places, and every extracted host doubles that. Miss
one and the module compiles, starts, and silently does nothing.

| # | File | What it registers |
|---|---|---|
| 1 | `Host/Dental.Api/Program.cs` | Mediator marker pair (`{Module}ContractsMarker`, `{Module}Module`) |
| 2 | `Host/Dental.Api/Program.cs` | the module assembly in `moduleAssemblies` |
| 3 | `Host/Dental.DbMigrator/Program.cs` | Mediator marker pair |
| 4 | `Host/Dental.DbMigrator/Program.cs` | the module assembly |
| 5–8 | the same four in `Dental.NotificationsHost` and `Dental.NotificationsMigrator` | for a module that host loads |

The sites are marked in the source with `REGISTRATION SITE n OF 4` comments. Follow them.

## The middleware order

`UseHeroMultiTenantDatabases()` runs **before** `UseHeroPlatform()`, and therefore before
authentication — which is why tenant resolution is header-driven rather than claim-driven. Inside
the platform the order is load-bearing:

```
CorrelationId → RequestLogging → Authentication → RateLimiting → Quota → Idempotency
  → Authorization → endpoint
```

Rate limiting sits after authentication so a signed-in caller is limited per user rather than per
IP. Idempotency sits before authorization so a replayed response is not re-authorized against a
token that has since changed.

## Running it

```bash
dotnet tool restore
dotnet build src/Dental.slnx                                  # warnings are errors
dotnet run --project src/Host/Dental.AppHost                  # everything, orchestrated
dotnet run --project src/Host/Dental.DbMigrator -- apply --seed
dotnet test --project src/Tests/Architecture.Tests/Dental.Architecture.Tests.csproj --no-build
```

`dotnet test` runs on Microsoft.Testing.Platform (the opt-in is in `global.json`) and takes
`--project`, not a path.

## Before you say you are done

Run `.agents/workflows/verify.md`. It is the same sequence CI runs.

## The rules

Read the one that matches what you are touching. They are short.

| File | When |
|---|---|
| `.agents/rules/architecture.md` | adding a module, moving code between modules, anything that crosses a boundary |
| `.agents/rules/persistence.md` | a DbContext, an entity, a query filter, a migration |
| `.agents/rules/eventing.md` | a domain event, an integration event, a handler |
| `.agents/rules/api.md` | an endpoint, a command, a query, a validator, an error |
| `.agents/rules/style.md` | any C# |
| `.agents/rules/frontend.md` | anything under `clients/` |
| `.agents/rules/testing.md` | any test |

## Skills

Step-by-step recipes for the things done often enough to get wrong:

* `.agents/skills/add-a-slice/SKILL.md` — a new endpoint in an existing module
* `.agents/skills/add-a-module/SKILL.md` — a new module, all four registration sites
* `.agents/skills/add-a-migration/SKILL.md` — the exact `dotnet ef` invocation and its traps
* `.agents/skills/add-an-integration-event/SKILL.md` — publishing across a module boundary

## The ten things that actually bite

1. A module registered in three of four places starts cleanly and does nothing.
2. `UseMultiTenant()` after `UseHeroPlatform()` gives every request the root tenant.
3. An `IOutboxStore` resolved without its context type writes into another module's change tracker.
4. Setting an AsyncLocal inside an awaited helper does not flow back to the caller: the tenant
   restorer resolves and applies in two steps for exactly this reason.
5. An inbox claim committed before the handler runs loses the event when the handler throws.
6. A background handler with no ambient tenant NREs inside a compiled query, not at the call site.
7. `builder.HasQueryFilter` overwrites: soft-delete uses a NAMED filter so Finbuckle's tenant filter
   survives.
8. ASP.NET Identity's `RoleNameIndex` and `UserNameIndex` are globally unique — fatal in a shared
   schema. They are removed from the model; the tenant-scoped ones live in `IdentityConfigurations`.
9. A `Program.cs` that reads configuration during registration ignores anything a test injects
   afterwards. Integration tests configure the host through environment variables.
10. Both SPAs are served from the API's origin: unnamespaced storage keys have them signing each
    other out.
