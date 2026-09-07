# Verify

The sequence CI runs. Run it before you say a change is done.

## Backend

```bash
dotnet tool restore
dotnet build src/Dental.slnx -warnaserror

dotnet test --project src/Tests/Architecture.Tests/Dental.Architecture.Tests.csproj --no-build
dotnet test --project src/Tests/Framework.Tests/Dental.Framework.Tests.csproj --no-build
dotnet test --project src/Tests/Modules.Patients.Tests/Dental.Modules.Patients.Tests.csproj --no-build
dotnet test --project src/Tests/Modules.Scheduling.Tests/Dental.Modules.Scheduling.Tests.csproj --no-build
dotnet test --project src/Tests/Modules.Billing.Tests/Dental.Modules.Billing.Tests.csproj --no-build
dotnet test --project src/Tests/Integration.Middleware.Tests/Dental.Integration.Middleware.Tests.csproj --no-build
dotnet test --project src/Tests/Integration.Tests/Dental.Integration.Tests.csproj --no-build
```

The last one needs a database. With a container runtime it starts its own; otherwise point it at an
existing server and it will create and drop its own databases:

```bash
DENTAL_TEST_POSTGRES="Host=127.0.0.1;Port=5432;Database=postgres;Username=dental;Password=dental" \
  dotnet test --project src/Tests/Integration.Tests/Dental.Integration.Tests.csproj --no-build
```

With neither, it skips — which is not the same as passing. Say so if that is what happened.

## Frontend

```bash
cd clients/admin      && npm ci && npm run lint && npm run build && npm run test:e2e
cd ../dashboard       && npm ci && npm run lint && npm run build && npm run test:e2e
```

## Migrations

If you touched a model:

```bash
dotnet run --project src/Host/Dental.DbMigrator -- list-pending
```

A model change with no migration is a deployment that fails at the first query.

## What "done" means

* The build is clean with warnings as errors.
* Every suite you could run has passed, and you have said which you could not run and why.
* A model change has a migration; a new module is in all four registration sites.
