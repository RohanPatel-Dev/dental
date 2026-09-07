using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Extensions;
using Dental.Framework.Persistence.Initialization;
using Dental.Framework.Shared.Identity;
using Dental.Framework.Web.Health;
using Dental.Framework.Web.Modules;
using Dental.Framework.Web.Platform;
using Dental.Modules.Notifications.Contracts.Authorization;
using Dental.Modules.Notifications.Data;
using Dental.Modules.Notifications.Features.v1.Notifications.SearchNotifications;
using Dental.Modules.Notifications.Jobs;
using Dental.Modules.Notifications.Services;
using Hangfire;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

[assembly: FshModule(typeof(Dental.Modules.Notifications.NotificationsModule), 940)]

namespace Dental.Modules.Notifications;

/// <summary>
/// Outbound patient contact (order 940).
/// </summary>
/// <remarks>
/// This is the module that runs in its own host. It is bursty - a morning reminder sweep for a large
/// group practice is thousands of messages - and it must not compete with interactive API traffic
/// for threads. Extracting it means its own <c>Program.cs</c> registration pair, its own migrations
/// project and migrator, its own Hangfire database, and RabbitMQ rather than the in-memory bus,
/// because the in-memory bus cannot cross a process boundary.
/// It is loaded by the main API too, so that the module's endpoints and event handlers are available
/// in a single-process development run; only the recurring dispatch job is host specific.
/// </remarks>
public sealed class NotificationsModule : IModule
{
    /// <summary>OpenAPI tag and group name for this module.</summary>
    public const string Tag = "Notifications";

    /// <inheritdoc />
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(NotificationsPermissions.All);

        builder.Services.AddOptions<NotificationOptions>()
            .BindConfiguration(nameof(NotificationOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddHeroDbContext<NotificationsDbContext>();
        builder.Services.AddScoped<IDbInitializer, NotificationsDbInitializer>();

        builder.Services.AddSingleton<NotificationComposer>();
        builder.Services.AddScoped<NotificationQueue>();
        builder.Services.AddScoped<NotificationDispatchJob>();

        builder.Services.AddEventingCore(builder.Configuration);
        builder.Services.AddEventingForDbContext<NotificationsDbContext>();
        builder.Services.AddIntegrationEventHandlers(typeof(NotificationsModule).Assembly);

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<NotificationsDbContext>(
                "db:notifications",
                tags: [HealthEndpoints.ReadyTag]);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapModuleGroup(Tag);
        group.MapSearchNotificationsEndpoint();

        RegisterRecurringJobs(endpoints);
    }

    private static void RegisterRecurringJobs(IEndpointRouteBuilder endpoints)
    {
        IRecurringJobManager? jobs = endpoints.ServiceProvider.GetService<IRecurringJobManager>();
        if (jobs is null)
        {
            return;
        }

        NotificationOptions options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<NotificationOptions>>().Value;

        jobs.AddOrUpdate<NotificationDispatchJob>(
            "notifications:dispatch",
            job => job.RunAsync(CancellationToken.None),
            options.DispatchCron,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}
