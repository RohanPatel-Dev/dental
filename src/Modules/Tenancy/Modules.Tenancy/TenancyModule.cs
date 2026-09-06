using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Extensions;
using Dental.Framework.Persistence.Initialization;
using Dental.Framework.Quota;
using Dental.Framework.Shared.Identity;
using Dental.Framework.Web.Health;
using Dental.Framework.Web.Modules;
using Dental.Framework.Web.Platform;
using Dental.Framework.Web.Tenancy;
using Dental.Modules.Tenancy.Contracts.Authorization;
using Dental.Modules.Tenancy.Contracts.Events;
using Dental.Modules.Tenancy.Contracts.Services;
using Dental.Modules.Tenancy.Data;
using Dental.Modules.Tenancy.Features.v1.Tenants.ChangeTenantPlan;
using Dental.Modules.Tenancy.Features.v1.Tenants.CreateTenant;
using Dental.Modules.Tenancy.Features.v1.Tenants.GetTenant;
using Dental.Modules.Tenancy.Features.v1.Tenants.SearchTenants;
using Dental.Modules.Tenancy.Features.v1.Tenants.SetTenantStatus;
using Dental.Modules.Tenancy.Services;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(Dental.Modules.Tenancy.TenancyModule), 100)]

namespace Dental.Modules.Tenancy;

/// <summary>
/// The tenant catalog. Loads first (order 100) because tenant resolution has to work before any
/// other module can read a row.
/// </summary>
public sealed class TenancyModule : IModule
{
    /// <summary>OpenAPI tag and group name for this module.</summary>
    public const string Tag = "Tenancy";

    /// <inheritdoc />
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(TenancyPermissions.All);

        builder.Services.AddHeroDbContext<TenancyDbContext>();
        builder.Services.AddScoped<IDbInitializer, TenancyDbInitializer>();

        builder.Services.AddScoped<ITenantService, TenantService>();

        // Finbuckle's store is resolved outside any request scope, so it is a singleton that opens
        // its own scope per lookup.
        builder.Services.AddSingleton<IMultiTenantStore<DentalTenantInfo>, EfTenantStore>();

        // Quota limits come from the tenant's plan; the framework default is only a fallback.
        builder.Services.Replace(
            ServiceDescriptor.Scoped<IQuotaLimitProvider, PlanQuotaLimitProvider>());

        builder.Services.AddEventingCore(builder.Configuration);
        builder.Services.AddEventingForDbContext<TenancyDbContext>();
        builder.Services.AddIntegrationEventHandlers(typeof(TenancyModule).Assembly);

        // Published here, handled elsewhere: without these markers the RabbitMQ consumer would
        // declare no queue for them and they would be published into the void.
        builder.Services.AddIntegrationEventType<TenantProvisionedIntegrationEvent>();
        builder.Services.AddIntegrationEventType<TenantDeactivatedIntegrationEvent>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<TenancyDbContext>("db:tenancy", tags: [HealthEndpoints.ReadyTag]);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapModuleGroup(Tag);

        // Literal-segment routes first. Registered after "/tenants/{identifier}" they would be
        // shadowed by it and never match.
        group.MapSearchTenantsEndpoint();
        group.MapCreateTenantEndpoint();
        group.MapSetTenantStatusEndpoint();
        group.MapChangeTenantPlanEndpoint();
        group.MapGetTenantEndpoint();
    }
}
