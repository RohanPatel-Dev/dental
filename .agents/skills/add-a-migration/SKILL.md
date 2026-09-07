---
name: add-a-migration
description: Generate and apply an EF Core migration for one module, with the traps that make it silently wrong.
---

# Add a migration

## Build first. Always.

`dotnet ef` works off the **compiled** model. Adding or removing a migration against a stale build
silently discards work.

```bash
dotnet tool restore
dotnet build src/Dental.slnx

dotnet ef migrations add DescribeTheChange \
  --project         src/Host/Dental.Migrations.PostgreSQL \
  --startup-project src/Host/Dental.Api \
  --context         PatientsDbContext \
  --output-dir      Patients
```

For the extracted host, swap in `Dental.Notifications.Migrations.PostgreSQL` and
`Dental.NotificationsHost`.

## Read what it generated

Open the migration before you keep it. Look for:

* an index or constraint you did not intend to drop — EF renames by dropping and recreating;
* a `TenantId` missing from a unique index (see `.agents/rules/persistence.md`);
* a rename EF has modelled as drop-and-add, which loses data. Rewrite it as `RenameColumn`.

## Removing one

```bash
dotnet build src/Dental.slnx      # again: the snapshot is compiled state
dotnet ef migrations remove --project ... --startup-project ... --context ...
```

`remove` refuses once a migration is applied to the database it can reach. If you have already
applied it locally, either revert the database or delete the two files and restore the snapshot from
git — do not leave a half-removed migration behind.

## Applying

Never at API startup:

```bash
dotnet run --project src/Host/Dental.DbMigrator -- apply --seed
dotnet run --project src/Host/Dental.DbMigrator -- list-pending
```

The migrator holds a Postgres advisory lock for the whole run, so a rolling deploy or a retried CI
job cannot race on the same DDL.
