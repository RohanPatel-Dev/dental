---
name: add-a-slice
description: Add an endpoint to an existing module - command or query, handler, validator, endpoint, permission, test.
---

# Add a slice

A slice is one folder. Everything the feature needs is in it, and nothing else changes.

## 1. Contracts

`src/Modules/{Name}/Modules.{Name}.Contracts/v1/{Area}/{Action}/{Action}Command.cs`

```csharp
public sealed record CancelAppointmentCommand(Guid AppointmentId, string Reason)
    : ICommand<AppointmentDto>;
```

A query is `IQuery<T>` and a paged query implements `IPagedQuery`. DTOs go in `Contracts/Dtos/`.
Contracts reference Mediator, the eventing abstractions and `Dental.Framework.Shared` — nothing else.

## 2. Permission

If the action needs a new permission, add it to the module's permission class in Contracts. It is
registered automatically from `ConfigureServices`.

## 3. Runtime

`src/Modules/{Name}/Modules.{Name}/Features/v1/{Area}/{Action}/` gets three files:

* `{Action}CommandHandler.cs` — sealed, primary constructor, returns a DTO. Throw
  `NotFoundException` / `ConflictException` and stop; do not build error responses.
* `{Action}CommandValidator.cs` — a `FluentValidation` validator. The pipeline runs it; the handler
  assumes valid input.
* `{Action}Endpoint.cs` — an `internal static` extension mapping one route, with
  `.WithName()`, `.WithSummary()`, `.Produces<T>()`, `.ProducesProblem()`, `.RequirePermission()`,
  and `.WithIdempotency()` on a create.

## 4. Map it

Add `group.Map{Action}Endpoint();` to the module's `MapEndpoints`. **Literal routes before
parameterised ones**, or `/appointments/search` is swallowed by `/appointments/{id:guid}`.

## 5. Test it

Domain rule → a unit test in `src/Tests/Modules.{Name}.Tests`. Anything that depends on the filter,
the pipeline or the database → `src/Tests/Integration.Tests`.

## 6. Verify

```bash
dotnet build src/Dental.slnx
dotnet test --project src/Tests/Architecture.Tests/Dental.Architecture.Tests.csproj --no-build
```

The architecture tests check the naming, the boundary and the authorization metadata for you.
