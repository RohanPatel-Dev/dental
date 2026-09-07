using System.Reflection;
using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Web.Migrations;
using Dental.Framework.Jobs;
using Dental.Framework.Web.Modules;
using Dental.Framework.Web.Platform;
using Dental.Framework.Web.Validation;
using Dental.Modules.Notifications;
using Dental.Modules.Notifications.Contracts;
using Dental.Modules.Tenancy;
using Dental.Modules.Tenancy.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// The extracted host's own migrator. It applies the migrations in
// Dental.Notifications.Migrations.PostgreSQL, which the main migrator never touches.
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// REGISTRATION SITE 7 OF 8: mirrored from Dental.NotificationsHost.
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

// REGISTRATION SITE 8 OF 8.
Assembly[] moduleAssemblies =
[
    typeof(TenancyModule).Assembly,
    typeof(NotificationsModule).Assembly,
];

builder.AddHeroPlatform(platform =>
{
    platform.EnableJobs = false;
    platform.EnableQuotas = false;
    platform.EnableSse = false;
    platform.EnableRateLimiting = false;
});

builder.AddModules(moduleAssemblies);
builder.Services.AddIntegrationEventTypeRegistry(moduleAssemblies, "dental-notifications-migrator");

builder.Services.AddNoOpJobs();
builder.Services.AddSingleton<MigrationRunner>();

using IHost host = builder.Build();

ILogger<MigrationRunner> logger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger<MigrationRunner>();

MigratorCommand command = MigratorCommand.Parse(args);

if (command.ShowHelp)
{
    await Console.Out.WriteLineAsync(MigratorCommand.HelpText).ConfigureAwait(false);
    return 0;
}

string connectionString =
    builder.Configuration["DatabaseOptions:ConnectionString"]
    ?? throw new InvalidOperationException("DatabaseOptions:ConnectionString is not configured.");

try
{
    await host.Services.GetRequiredService<MigrationRunner>()
        .RunAsync(
            command.ToRunOptions() with { Seed = command.Verb != MigratorVerb.Apply || command.Seed },
            connectionString,
            CancellationToken.None)
        .ConfigureAwait(false);
}
catch (Exception exception)
{
    logger.LogCritical(exception, "The notifications migrator failed. No further changes were applied.");
    return 1;
}

logger.LogInformation("Notifications migrator finished successfully.");
return 0;
