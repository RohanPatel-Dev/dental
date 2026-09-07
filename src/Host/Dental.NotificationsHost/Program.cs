using System.Reflection;
using System.Text.Json.Serialization;
using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Web.Modules;
using Dental.Framework.Web.Platform;
using Dental.Framework.Web.Validation;
using Dental.Modules.Notifications;
using Dental.Modules.Notifications.Contracts;
using Dental.Modules.Tenancy;
using Dental.Modules.Tenancy.Contracts;
using Microsoft.Extensions.DependencyInjection;

// The extracted host.
//
// It exists because reminder dispatch is bursty and must not compete with interactive API traffic
// for threads. Extraction is not free - it costs this file, its own migrations project, its own
// migrator, its own Hangfire database, and RabbitMQ instead of the in-memory bus - so it is done
// only when independent scaling actually justifies that.
//
// Tenancy is loaded alongside Notifications because tenant resolution has to work in every process.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(json =>
    json.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.ValidateProductionConfiguration();

// REGISTRATION SITE 5 OF 8: this host's Mediator marker pairs.
builder.Services.AddMediator(mediator =>
{
    mediator.ServiceLifetime = ServiceLifetime.Scoped;
    mediator.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
    mediator.Assemblies =
    [
        typeof(TenancyContractsMarker),
        typeof(TenancyModule),
        typeof(NotificationsContractsMarker),
        typeof(NotificationsModule),
    ];
});

// REGISTRATION SITE 6 OF 8: this host's module assemblies.
Assembly[] moduleAssemblies =
[
    typeof(TenancyModule).Assembly,
    typeof(NotificationsModule).Assembly,
];

builder.AddHeroPlatform(platform =>
{
    platform.EnableJobs = true;
    platform.EnableQuotas = false;
    platform.EnableSse = false;

    platform.ConsumerName = "dental-notifications";
});

builder.AddModules(moduleAssemblies);

// Queue names are keyed by the CONSUMER, so this host competes with nothing and gets its own
// durable queues per event type.
builder.Services.AddIntegrationEventTypeRegistry(moduleAssemblies, "dental-notifications");

builder.Services.AddResponseCompression();

WebApplication app = builder.Build();

app.UseHeroMultiTenantDatabases();

app.UseHeroPlatform(pipeline =>
{
    pipeline.MapOpenApi = false;

    // Headless: services registered, hub not mapped. See the comment on EnableRealtime above.
    pipeline.MapRealtime = false;

    pipeline.MapSse = false;
    pipeline.MapJobsDashboard = true;
});

await app.RunAsync().ConfigureAwait(false);

/// <summary>Entry point marker for the integration tests.</summary>
public partial class Program
{
    /// <summary>Never instantiated; the type exists only as a generic argument for the test factory.</summary>
    protected Program()
    {
    }
}
