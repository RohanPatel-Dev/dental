using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Extensions;
using Dental.Framework.Persistence.Initialization;
using Dental.Framework.Shared.Identity;
using Dental.Framework.Web.Health;
using Dental.Framework.Web.Modules;
using Dental.Framework.Web.Platform;
using Dental.Modules.Clinical.Contracts.Authorization;
using Dental.Modules.Clinical.Contracts.Events;
using Dental.Modules.Clinical.Contracts.Services;
using Dental.Modules.Clinical.Data;
using Dental.Modules.Clinical.Features.v1.ChartEntries.GetToothChart;
using Dental.Modules.Clinical.Features.v1.ChartEntries.RecordChartEntry;
using Dental.Modules.Clinical.Features.v1.Procedures.CreateProcedure;
using Dental.Modules.Clinical.Features.v1.Procedures.ListProcedures;
using Dental.Modules.Clinical.Features.v1.TreatmentPlans.AcceptTreatmentPlan;
using Dental.Modules.Clinical.Features.v1.TreatmentPlans.CreateTreatmentPlan;
using Dental.Modules.Clinical.Features.v1.TreatmentPlans.GetTreatmentPlan;
using Dental.Modules.Clinical.Features.v1.TreatmentPlans.SearchTreatmentPlans;
using Dental.Modules.Clinical.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(Dental.Modules.Clinical.ClinicalModule), 920)]

namespace Dental.Modules.Clinical;

/// <summary>
/// The clinical record (order 920): procedure catalog, treatment plans and the tooth chart.
/// </summary>
public sealed class ClinicalModule : IModule
{
    /// <summary>OpenAPI tag and group name for this module.</summary>
    public const string Tag = "Clinical";

    /// <inheritdoc />
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(ClinicalPermissions.All);

        builder.Services.AddHeroDbContext<ClinicalDbContext>();
        builder.Services.AddScoped<IDbInitializer, ClinicalDbInitializer>();

        builder.Services.AddScoped<ITreatmentPlanService, TreatmentPlanService>();

        builder.Services.AddEventingCore(builder.Configuration);
        builder.Services.AddEventingForDbContext<ClinicalDbContext>();
        builder.Services.AddIntegrationEventHandlers(typeof(ClinicalModule).Assembly);

        builder.Services.AddIntegrationEventType<ProceduresDeliveredIntegrationEvent>();
        builder.Services.AddIntegrationEventType<TreatmentPlanAcceptedIntegrationEvent>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ClinicalDbContext>("db:clinical", tags: [HealthEndpoints.ReadyTag]);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapModuleGroup(Tag);

        group.MapListProceduresEndpoint();
        group.MapCreateProcedureEndpoint();

        group.MapSearchTreatmentPlansEndpoint();
        group.MapCreateTreatmentPlanEndpoint();
        group.MapGetTreatmentPlanEndpoint();
        group.MapAcceptTreatmentPlanEndpoint();

        group.MapRecordChartEntryEndpoint();
        group.MapGetToothChartEndpoint();
    }
}
