using Dental.Framework.Core.Contracts;
using Dental.Framework.Persistence.Extensions;
using Dental.Framework.Persistence.Initialization;
using Dental.Framework.Shared.Identity;
using Dental.Framework.Web.Health;
using Dental.Framework.Web.Modules;
using Dental.Framework.Web.Platform;
using Dental.Modules.Auditing.Contracts.Authorization;
using Dental.Modules.Auditing.Data;
using Dental.Modules.Auditing.Features.v1.AuditTrails.SearchAuditTrails;
using Dental.Modules.Auditing.Jobs;
using Dental.Modules.Auditing.Services;
using Hangfire;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

[assembly: FshModule(typeof(Dental.Modules.Auditing.AuditingModule), 300)]

namespace Dental.Modules.Auditing;

/// <summary>
/// The append-only audit trail. Loads after Tenancy and Identity (order 300) so that the sink is
/// registered before any business module can produce a change worth recording.
/// </summary>
public sealed class AuditingModule : IModule
{
    /// <summary>OpenAPI tag and group name for this module.</summary>
    public const string Tag = "Auditing";

    /// <inheritdoc />
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(AuditingPermissions.All);

        builder.Services.AddOptions<AuditingOptions>()
            .BindConfiguration(nameof(AuditingOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddHeroDbContext<AuditingDbContext>();
        builder.Services.AddScoped<IDbInitializer, AuditingDbInitializer>();

        // Supplying this is what turns the framework's audit interceptor on. A host that does not
        // load this module simply produces no audit trail, with no other behaviour change.
        builder.Services.AddScoped<IAuditSink, AuditSink>();

        builder.Services.AddScoped<AuditRetentionJob>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<AuditingDbContext>("db:auditing", tags: [HealthEndpoints.ReadyTag]);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapModuleGroup(Tag);
        group.MapSearchAuditTrailsEndpoint();

        RegisterRecurringJobs(endpoints);
    }

    private static void RegisterRecurringJobs(IEndpointRouteBuilder endpoints)
    {
        // Recurring jobs have no IJobService API, so they are registered here through Hangfire's
        // own manager - and only when this host actually runs a Hangfire server.
        IRecurringJobManager? jobs = endpoints.ServiceProvider.GetService<IRecurringJobManager>();
        if (jobs is null)
        {
            return;
        }

        AuditingOptions options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<AuditingOptions>>().Value;

        jobs.AddOrUpdate<AuditRetentionJob>(
            "auditing:retention",
            job => job.RunAsync(CancellationToken.None),
            options.RetentionCron,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}
