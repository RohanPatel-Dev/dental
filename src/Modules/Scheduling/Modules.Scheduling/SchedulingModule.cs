using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Extensions;
using Dental.Framework.Persistence.Initialization;
using Dental.Framework.Shared.Identity;
using Dental.Framework.Web.Health;
using Dental.Framework.Web.Modules;
using Dental.Framework.Web.Platform;
using Dental.Modules.Scheduling.Contracts.Authorization;
using Dental.Modules.Scheduling.Contracts.Events;
using Dental.Modules.Scheduling.Contracts.Services;
using Dental.Modules.Scheduling.Data;
using Dental.Modules.Scheduling.Features.v1.Appointments.BookAppointment;
using Dental.Modules.Scheduling.Features.v1.Appointments.CancelAppointment;
using Dental.Modules.Scheduling.Features.v1.Appointments.CompleteAppointment;
using Dental.Modules.Scheduling.Features.v1.Appointments.GetAppointment;
using Dental.Modules.Scheduling.Features.v1.Appointments.RescheduleAppointment;
using Dental.Modules.Scheduling.Features.v1.Appointments.SearchAppointments;
using Dental.Modules.Scheduling.Features.v1.Providers.CreateProvider;
using Dental.Modules.Scheduling.Features.v1.Providers.ListProviders;
using Dental.Modules.Scheduling.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(Dental.Modules.Scheduling.SchedulingModule), 910)]

namespace Dental.Modules.Scheduling;

/// <summary>The appointment book (order 910). Depends on Patients only through its contract.</summary>
public sealed class SchedulingModule : IModule
{
    /// <summary>OpenAPI tag and group name for this module.</summary>
    public const string Tag = "Scheduling";

    /// <inheritdoc />
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(SchedulingPermissions.All);

        builder.Services.AddHeroDbContext<SchedulingDbContext>();
        builder.Services.AddScoped<IDbInitializer, SchedulingDbInitializer>();

        builder.Services.AddScoped<IAppointmentService, AppointmentService>();
        builder.Services.AddScoped<AppointmentService>();
        builder.Services.AddScoped<SlotAvailabilityChecker>();

        builder.Services.AddEventingCore(builder.Configuration);
        builder.Services.AddEventingForDbContext<SchedulingDbContext>();
        builder.Services.AddIntegrationEventHandlers(typeof(SchedulingModule).Assembly);

        builder.Services.AddIntegrationEventType<AppointmentBookedIntegrationEvent>();
        builder.Services.AddIntegrationEventType<AppointmentCancelledIntegrationEvent>();
        builder.Services.AddIntegrationEventType<AppointmentCompletedIntegrationEvent>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<SchedulingDbContext>("db:scheduling", tags: [HealthEndpoints.ReadyTag]);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapModuleGroup(Tag);

        group.MapSearchAppointmentsEndpoint();
        group.MapBookAppointmentEndpoint();
        group.MapGetAppointmentEndpoint();
        group.MapRescheduleAppointmentEndpoint();
        group.MapCancelAppointmentEndpoint();
        group.MapCompleteAppointmentEndpoint();

        group.MapListProvidersEndpoint();
        group.MapCreateProviderEndpoint();
    }
}
