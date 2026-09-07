---
name: add-a-module
description: Add a module to the monolith, including all four registration sites and its migrations folder.
---

# Add a module

Two projects, one `IModule`, four registration sites, one migrations folder. Miss the registration
and the module compiles, starts, and does nothing at all.

## 1. Projects

```
src/Modules/{Name}/Modules.{Name}/Dental.Modules.{Name}.csproj
src/Modules/{Name}/Modules.{Name}.Contracts/Dental.Modules.{Name}.Contracts.csproj
```

Add both to `src/Dental.slnx`. The runtime project references its own Contracts and whichever
**other modules' Contracts** it needs — never another module's runtime project.

Contracts contains a marker: `public sealed class {Name}ContractsMarker;`

## 2. The module class

```csharp
[assembly: FshModule("Scheduling", Order = 910)]

public sealed class SchedulingModule : IModule
{
    public const string Tag = "Scheduling";

    public void ConfigureServices(IHostApplicationBuilder builder) { /* context, services, permissions */ }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) { /* the slices */ }
}
```

`Order` decides both registration and migration order. Foundations are 100–300; business modules
start at 900.

## 3. Data

* `{Name}DbContext : BaseDbContext`, `public const string SchemaName = "{name}";`
* Register it with `AddHeroDbContext<{Name}DbContext>()` and a health check tagged
  `HealthEndpoints.ReadyTag`.
* Call `builder.ApplyEventingModel()` in `OnModelCreating` if the module publishes or handles
  integration events, and `ConfigureMultiTenant()` **last**.

## 4. The four registration sites

In **both** `Host/Dental.Api/Program.cs` and `Host/Dental.DbMigrator/Program.cs`:

* add `typeof({Name}ContractsMarker)` and `typeof({Name}Module)` to `mediator.Assemblies`;
* add `typeof({Name}Module).Assembly` to `moduleAssemblies`.

Follow the `REGISTRATION SITE n OF 4` comments. If the module will also load in an extracted host,
that host and its migrator have the same four again.

## 5. Migrations

Create `src/Host/Dental.Migrations.PostgreSQL/{Name}/` and add the first migration — see
`.agents/skills/add-a-migration/SKILL.md`.

## 6. Verify

```bash
dotnet build src/Dental.slnx
dotnet run --project src/Host/Dental.DbMigrator -- list-pending
dotnet test --project src/Tests/Architecture.Tests/Dental.Architecture.Tests.csproj --no-build
```

`ModuleRegistrationTests` checks that every assembly carrying `FshModule` has exactly one `IModule`
and a unique order.
