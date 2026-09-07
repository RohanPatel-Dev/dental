# Dental

Practice-management for dental practices, built as a **modular monolith with vertical slices** on
.NET 10 and React 19. One deployable API that loads every module, plus the ability to lift a module
into its own process without changing its code — which Notifications already does.

Multi-tenant by default: one deployment serves many practices, and every tenant-owned row is
filtered on the tenant the request resolved.

## What is in the box

| Module | Order | What it owns |
|---|---|---|
| Tenancy | 100 | The practices on the platform, their plans and their quotas |
| Identity | 200 | Users, roles, permissions, tokens |
| Auditing | 300 | The trail of who changed what |
| Patients | 900 | Patient records, documents, consent and erasure |
| Scheduling | 910 | Providers, operatories and the diary |
| Clinical | 920 | Procedures, treatment plans and chart entries |
| Billing | 930 | Invoices and payments |
| Notifications | 940 | Reminders and confirmations — **runs in its own host** |

Two SPAs: an operator console (`clients/admin`) and a practice app (`clients/dashboard`).

## Running it

```bash
dotnet tool restore
dotnet run --project src/Host/Dental.AppHost
```

Aspire starts Postgres, Valkey, RabbitMQ and MinIO, runs both migrators to completion, then both API
hosts and both SPAs, and gives you one dashboard over the lot.

Without Aspire:

```bash
dotnet run --project src/Host/Dental.DbMigrator -- apply --seed   # never at API startup
dotnet run --project src/Host/Dental.Api
cd clients/dashboard && npm install && npm run dev
```

## Testing

```bash
dotnet build src/Dental.slnx
dotnet test --project src/Tests/Architecture.Tests/Dental.Architecture.Tests.csproj --no-build
```

See `src/Tests/README.md` for what each suite covers and what the integration suite needs. The full
pre-flight sequence is `.agents/workflows/verify.md`.

## Deploying

`deploy/README.md`. One machine with Docker Compose, or Azure Container Apps with Terraform. In both
cases the migrator runs to completion **before** the API rolls onto the new image.

## Working in this repository

Read **[AGENTS.md](./AGENTS.md)**. It has the layout, the four registration sites that catch
everybody, the middleware order, and the ten things that actually bite — plus rules per area and
step-by-step recipes under `.agents/`.

## The shape of a feature

Everything one feature needs is in one folder:

```
src/Modules/Scheduling/
  Modules.Scheduling.Contracts/v1/Appointments/BookAppointment/BookAppointmentCommand.cs
  Modules.Scheduling/Features/v1/Appointments/BookAppointment/
    BookAppointmentCommandHandler.cs
    BookAppointmentCommandValidator.cs
    BookAppointmentEndpoint.cs
```

No repositories, no service layer, no four-project ceremony. A module talks to another module
through an integration event, or through a small contract service when it needs an answer now —
never through another module's `DbContext`.
