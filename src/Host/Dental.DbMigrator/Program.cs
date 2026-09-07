using System.Reflection;
using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Web.Migrations;
using Dental.Framework.Jobs;
using Dental.Framework.Persistence.Initialization;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// REGISTRATION SITE 3 OF 4: the Mediator marker pair, mirrored from Dental.Api. A module missing
// here migrates and seeds nothing, silently.
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

// REGISTRATION SITE 4 OF 4: the module assemblies, mirrored from Dental.Api.
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

// This process migrates and exits, so it runs no Hangfire server, enforces no quotas and serves no
// requests. The service abstractions modules depend on are still registered by the platform.
builder.AddHeroPlatform(platform =>
{
    platform.EnableJobs = false;
    platform.EnableQuotas = false;
    platform.EnableSse = false;
    platform.EnableRateLimiting = false;
});

builder.AddModules(moduleAssemblies);
builder.Services.AddIntegrationEventTypeRegistry(moduleAssemblies, "dental-migrator");

// A seeder that enqueues a job would write work no running host picks up. This makes that a loud
// failure during migration rather than a silent no-op in production.
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
    switch (command.Verb)
    {
        case MigratorVerb.Apply:
            await host.Services.GetRequiredService<MigrationRunner>()
                .RunAsync(command.ToRunOptions(), connectionString, CancellationToken.None)
                .ConfigureAwait(false);
            break;

        case MigratorVerb.Seed:
            await host.Services.GetRequiredService<MigrationRunner>()
                .RunAsync(command.ToRunOptions() with { Seed = true }, connectionString, CancellationToken.None)
                .ConfigureAwait(false);
            break;

        case MigratorVerb.SeedDemo:
            await host.Services.GetRequiredService<MigrationRunner>()
                .RunAsync(
                    command.ToRunOptions() with { Seed = true, SeedDemo = true },
                    connectionString,
                    CancellationToken.None)
                .ConfigureAwait(false);
            break;

        case MigratorVerb.ListPending:
            await PendingMigrationReporter
                .ReportAsync(host.Services, logger, CancellationToken.None)
                .ConfigureAwait(false);
            break;

        default:
            await Console.Error.WriteLineAsync(MigratorCommand.HelpText).ConfigureAwait(false);
            return 1;
    }
}
catch (Exception exception)
{
    logger.LogCritical(exception, "The migrator failed. No further changes were applied.");
    return 1;
}

logger.LogInformation("Migrator finished successfully.");
return 0;
