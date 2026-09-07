using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Extensions;
using Dental.Framework.Persistence.Initialization;
using Dental.Framework.Quota;
using Dental.Framework.Shared.Identity;
using Dental.Framework.Web.Health;
using Dental.Framework.Web.Modules;
using Dental.Framework.Web.Platform;
using Dental.Modules.Patients.Contracts.Authorization;
using Dental.Modules.Patients.Contracts.Events;
using Dental.Modules.Patients.Contracts.Services;
using Dental.Modules.Patients.Data;
using Dental.Modules.Patients.Features.v1.Patients.ErasePatient;
using Dental.Modules.Patients.Features.v1.Patients.GetPatient;
using Dental.Modules.Patients.Features.v1.Patients.RegisterPatient;
using Dental.Modules.Patients.Features.v1.Patients.SearchPatients;
using Dental.Modules.Patients.Features.v1.Patients.UpdateConsent;
using Dental.Modules.Patients.Features.v1.Patients.UpdatePatient;
using Dental.Modules.Patients.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(Dental.Modules.Patients.PatientsModule), 900)]

namespace Dental.Modules.Patients;

/// <summary>
/// The patient register. First of the business modules (order 900); everything clinical, scheduled
/// or billed hangs off a patient record.
/// </summary>
public sealed class PatientsModule : IModule
{
    /// <summary>OpenAPI tag and group name for this module.</summary>
    public const string Tag = "Patients";

    /// <inheritdoc />
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(PatientsPermissions.All);

        builder.Services.AddHeroDbContext<PatientsDbContext>();
        builder.Services.AddScoped<IDbInitializer, PatientsDbInitializer>();

        builder.Services.AddScoped<IPatientService, PatientService>();
        builder.Services.AddScoped<ChartNumberGenerator>();
        builder.Services.AddScoped<IQuotaGaugeProvider, PatientQuotaGaugeProvider>();

        builder.Services.AddEventingCore(builder.Configuration);
        builder.Services.AddEventingForDbContext<PatientsDbContext>();
        builder.Services.AddIntegrationEventHandlers(typeof(PatientsModule).Assembly);

        builder.Services.AddIntegrationEventType<PatientRegisteredIntegrationEvent>();
        builder.Services.AddIntegrationEventType<PatientErasureRequestedIntegrationEvent>();
        builder.Services.AddIntegrationEventType<PatientConsentChangedIntegrationEvent>();
        builder.Services.AddIntegrationEventType<PatientContactChangedIntegrationEvent>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<PatientsDbContext>("db:patients", tags: [HealthEndpoints.ReadyTag]);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapModuleGroup(Tag);

        // Collection routes before the "{id:guid}" routes.
        group.MapSearchPatientsEndpoint();
        group.MapRegisterPatientEndpoint();
        group.MapUpdateConsentEndpoint();
        group.MapGetPatientEndpoint();
        group.MapUpdatePatientEndpoint();
        group.MapErasePatientEndpoint();
    }
}
