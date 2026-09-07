# Persistence rules

## Contexts

* One `DbContext` per module, one schema per module, derived from `BaseDbContext`.
* The exception is `IdentityModuleDbContext`: ASP.NET Identity requires
  `IdentityDbContext<TUser,TRole,TKey>` and C# has no multiple inheritance, so it implements
  `IMultiTenantDbContext` by hand and applies the same conventions explicitly. If you change
  `BaseDbContext`, check that context too.
* `OnModelCreating` order matters: conventions first, `ConfigureMultiTenant()` **last**.

## Tenancy

* Entities inherit `BaseEntity` (UUIDv7 id, timestamps, `TenantId`).
* An entity that belongs to no tenant implements `IGlobalEntity` — tenants, plans, outbox and inbox
  rows. Everything else is tenant-scoped and Finbuckle filters it.
* `EnforceMultiTenant()` refuses a write whose `TenantId` is not the context's. That is the design,
  not an obstacle: if you hit it, you are writing another tenant's data. Fix the caller. Auditing
  learned this the hard way — the trail row belongs to the tenant that made the change, not to the
  tenant the changed row names.
* To read across tenants deliberately, use `IgnoreQueryFilters()` and say why in a comment.

## Query filters

`HasQueryFilter` **overwrites**. Soft delete uses a named filter (`BaseDbContext.SoftDeleteFilterName`)
so Finbuckle's anonymous tenant filter survives alongside it. Never add an unnamed filter to an
entity that is tenant-scoped.

## Migrations

* Never at startup. `Dental.DbMigrator` applies them, under a Postgres advisory lock so two
  migrators cannot race.
* One folder per module inside the migrations project, each with its own model snapshot.
* **Build before `dotnet ef migrations add`, and build before `migrations remove`** — both work off
  the compiled model, and a stale snapshot silently discards work.
* See `.agents/skills/add-a-migration/SKILL.md`.

## Indexes

Anything unique must include `TenantId` unless the entity is `IGlobalEntity`. A globally unique
index in a shared schema means the second practice cannot have what the first one has.
