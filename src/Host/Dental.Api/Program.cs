using System.Reflection;
using System.Text.Json.Serialization;
using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Web.Modules;
using Dental.Framework.Web.Platform;
using Dental.Framework.Web.Validation;
using Dental.Modules.Auditing;
using Dental.Modules.Auditing.Contracts;
using Dental.Modules.Billing;
using Dental.Modules.Billing.Contracts;
using Dental.Modules.Clinical;
using Dental.Modules.Clinical.Contracts;
using Dental.Modules.Identity;
using Dental.Modules.Identity.Contracts;
using Dental.Modules.Patients;
using Dental.Modules.Patients.Contracts;
using Dental.Modules.Scheduling;
using Dental.Modules.Scheduling.Contracts;
using Dental.Modules.Tenancy;
using Dental.Modules.Tenancy.Contracts;
using Microsoft.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 1. Enums serialize as string names, so the two SPAs can mirror them as TypeScript string unions
//    instead of tracking ordinals that shift whenever a member is inserted.
builder.Services.ConfigureHttpJsonOptions(json =>
    json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// 2. Refuse to start in Production without the settings the app cannot work without. A process that
//    boots and then fails every request is worse than one that never boots.
builder.ValidateProductionConfiguration();

// 3. Mediator needs TWO markers per module - the Contracts assembly, where the messages live, and
//    the module assembly, where the handlers live. Omit either and the handler is silently never
//    discovered; the failure surfaces as "no handler registered" on the first request that needs it.
//    THIS IS REGISTRATION SITE 1 OF 4. See docs/architecture.md.
builder.Services.AddMediator(mediator =>
{
    mediator.ServiceLifetime = ServiceLifetime.Scoped;
    mediator.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
    mediator.Assemblies =
    [
        typeof(TenancyContractsMarker),
        typeof(TenancyModule),
        typeof(IdentityContractsMarker),
        typeof(IdentityModule),
        typeof(AuditingContractsMarker),
        typeof(AuditingModule),
        typeof(PatientsContractsMarker),
        typeof(PatientsModule),
        typeof(SchedulingContractsMarker),
        typeof(SchedulingModule),
        typeof(ClinicalContractsMarker),
        typeof(ClinicalModule),
        typeof(BillingContractsMarker),
        typeof(BillingModule),
    ];
});

// 4. The module assemblies the loader scans for [FshModule].
//    Notifications is deliberately ABSENT: it runs in Dental.NotificationsHost, with its own
//    migrations project and its own migrator. Adding it here would give this process a
//    NotificationsDbContext pointed at a migrations assembly that holds no Notifications migrations.
//    THIS IS REGISTRATION SITE 2 OF 4. Miss a module here and it never loads: no endpoints, no
//    DbContext, no error.
Assembly[] moduleAssemblies =
[
    typeof(TenancyModule).Assembly,
    typeof(IdentityModule).Assembly,
    typeof(AuditingModule).Assembly,
    typeof(PatientsModule).Assembly,
    typeof(SchedulingModule).Assembly,
    typeof(ClinicalModule).Assembly,
    typeof(BillingModule).Assembly,
];

// 5. The platform: auth, CORS, versioning, OpenAPI, health, observability and every enabled subsystem.
builder.AddHeroPlatform(platform =>
{
    platform.EnableJobs = true;
    platform.EnableQuotas = true;
    platform.EnableSse = true;
    platform.ConsumerName = "dental-api";
});

// 6. Load the modules: DI registration for each, in declared order.
builder.AddModules(moduleAssemblies);

builder.Services.AddIntegrationEventTypeRegistry(moduleAssemblies, "dental-api");

builder.Services.AddResponseCompression();

// 7. Host-scoped hosted services go HERE, not in a module: a module registering one would give
//    every host that loads it its own redundant copy of the same singleton work.
builder.Services.AddHostedService<Dental.Api.ExpiredRefreshTokenSweeper>();

WebApplication app = builder.Build();

// Tenant resolution runs BEFORE UseHeroPlatform, and therefore before authentication - which is why
// it is header driven rather than claim driven.
app.UseHeroMultiTenantDatabases();

app.UseHeroPlatform(pipeline =>
{
    pipeline.UseStaticFiles = false;
    pipeline.MapOpenApi = true;
    pipeline.MapRealtime = true;
    pipeline.MapSse = true;
    pipeline.MapJobsDashboard = true;
});

await app.RunAsync().ConfigureAwait(false);

/// <summary>Entry point marker, so the integration tests can build a <c>WebApplicationFactory</c>.</summary>
public partial class Program
{
    /// <summary>Never instantiated; the type exists only as a generic argument for the test factory.</summary>
    protected Program()
    {
    }
}
