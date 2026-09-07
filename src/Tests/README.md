# Tests

| Project | What it covers | Needs |
|---|---|---|
| `Architecture.Tests` | The rules that keep the monolith modular: module boundaries, handler conventions, tenant markers, authorization metadata, module registration, naming. | nothing |
| `Framework.Tests` | BuildingBlocks units: the permission registry under concurrency, the value objects, outbox serialization, SSE tokens, quota accounting. | nothing |
| `Modules.*.Tests` | Domain rules that belong to an aggregate: appointment slots, invoice money, patient erasure, chart numbering. | nothing |
| `Integration.Middleware.Tests` | Middleware behaviour and order, against a hand-built pipeline. | nothing |
| `Integration.Tests` | The real API host against a real Postgres: sign-in, permissions, paging, idempotent replay, tenant provisioning through the outbox, and cross-tenant isolation. | Postgres |

## Running them

```bash
dotnet build src/Dental.slnx
dotnet test --project src/Tests/Framework.Tests/Dental.Framework.Tests.csproj --no-build
```

`dotnet test` needs the Microsoft.Testing.Platform opt-in in `global.json` (already there) and takes
`--project`, not a path argument.

## The database the integration tests need

By default `Integration.Tests` starts Postgres with Testcontainers. Without a container runtime the
whole collection SKIPS rather than fails - a red suite that only means "no Docker here" trains
people to ignore red suites.

On a machine that has Postgres but no container runtime, point the suite at it instead:

```bash
DENTAL_TEST_POSTGRES="Host=127.0.0.1;Port=5432;Database=postgres;Username=dental;Password=dental" \
  dotnet test --project src/Tests/Integration.Tests/Dental.Integration.Tests.csproj --no-build
```

The connection string names an admin database; the fixture creates its own databases per run and
drops them afterwards, so it never touches an existing one.

## Coverage

```bash
dotnet test --project src/Tests/Framework.Tests/Dental.Framework.Tests.csproj \
  --settings src/coverage.runsettings
```
