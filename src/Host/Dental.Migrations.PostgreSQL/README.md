# Dental.Migrations.PostgreSQL

Every EF Core migration for the **main API host** lives here, organised one folder per module, each
with its own `{Module}DbContextModelSnapshot`.

They are in a single project because `AddHeroDbContext<T>()` resolves one process-wide
`DatabaseOptions:MigrationsAssembly`. A host that needs a different set of migrations therefore has
to be a different process with its own migrations project and its own migrator - which is exactly
what `Dental.Notifications.Migrations.PostgreSQL` is for.

## Adding a migration

```bash
dotnet tool restore
dotnet build src/Dental.slnx                     # ALWAYS build first
dotnet ef migrations add AddSomething \
  --project        src/Host/Dental.Migrations.PostgreSQL \
  --startup-project src/Host/Dental.Api \
  --context        PatientsDbContext \
  --output-dir     Patients
```

`dotnet ef migrations remove` operates on the snapshot, so a stale snapshot silently discards work.
Build before you add, and build before you remove.

## Applying migrations

Never at API startup. The one-shot migrator does it:

```bash
dotnet run --project src/Host/Dental.DbMigrator -- apply --seed
```

## What lives here

| Folder | Context | Schema |
|---|---|---|
| `Tenancy` | `TenancyDbContext` | `tenancy` |
| `Identity` | `IdentityModuleDbContext` | `identity` |
| `Auditing` | `AuditingDbContext` | `auditing` |
| `Patients` | `PatientsDbContext` | `patients` |
| `Scheduling` | `SchedulingDbContext` | `scheduling` |
| `Clinical` | `ClinicalDbContext` | `clinical` |
| `Billing` | `BillingDbContext` | `billing` |

`NotificationsDbContext` is deliberately absent: that module runs in `Dental.NotificationsHost` and
its migrations live in `Dental.Notifications.Migrations.PostgreSQL`.
