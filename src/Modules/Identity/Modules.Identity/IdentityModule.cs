using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Extensions;
using Dental.Framework.Persistence.Initialization;
using Dental.Framework.Quota;
using Dental.Framework.Shared.Identity;
using Dental.Framework.Web.Health;
using Dental.Framework.Web.Modules;
using Dental.Framework.Web.Platform;
using Dental.Modules.Identity.Contracts.Authorization;
using Dental.Modules.Identity.Contracts.Events;
using Dental.Modules.Identity.Contracts.Services;
using Dental.Modules.Identity.Data;
using Dental.Modules.Identity.Domain;
using Dental.Modules.Identity.Features.v1.Account.GetCurrentUser;
using Dental.Modules.Identity.Features.v1.Roles.CreateRole;
using Dental.Modules.Identity.Features.v1.Roles.ListRoles;
using Dental.Modules.Identity.Features.v1.Roles.UpdateRolePermissions;
using Dental.Modules.Identity.Features.v1.Tokens.IssueToken;
using Dental.Modules.Identity.Features.v1.Tokens.RefreshToken;
using Dental.Modules.Identity.Features.v1.Users.AssignRoles;
using Dental.Modules.Identity.Features.v1.Users.CreateUser;
using Dental.Modules.Identity.Features.v1.Users.GetUser;
using Dental.Modules.Identity.Features.v1.Users.SearchUsers;
using Dental.Modules.Identity.Features.v1.Users.SetUserStatus;
using Dental.Modules.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(Dental.Modules.Identity.IdentityModule), 200)]

namespace Dental.Modules.Identity;

/// <summary>
/// Users, roles and fine grained permissions. Loads right after Tenancy (order 200) because every
/// other module's authorization depends on it.
/// </summary>
public sealed class IdentityModule : IModule
{
    /// <summary>OpenAPI tag and group name for this module.</summary>
    public const string Tag = "Identity";

    /// <inheritdoc />
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(IdentityPermissions.All);

        builder.Services.AddHeroDbContext<IdentityModuleDbContext>();
        builder.Services.AddScoped<IDbInitializer, IdentityDbInitializer>();

        builder.Services.AddOptions<IdentitySeedOptions>()
            .BindConfiguration(nameof(IdentitySeedOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services
            .AddIdentityCore<DentalUser>(identity =>
            {
                identity.User.RequireUniqueEmail = false; // Unique per tenant, not globally.
                identity.Password.RequiredLength = 12;
                identity.Password.RequireDigit = true;
                identity.Password.RequireUppercase = true;
                identity.Password.RequireLowercase = true;
                identity.Password.RequireNonAlphanumeric = false;
                identity.Lockout.MaxFailedAccessAttempts = 5;
                identity.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<DentalRole>()
            .AddEntityFrameworkStores<IdentityModuleDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<IdentitySeeder>();
        builder.Services.AddSingleton<TokenService>();
        builder.Services.AddScoped<IQuotaGaugeProvider, UserQuotaGaugeProvider>();

        builder.Services.AddEventingCore(builder.Configuration);
        builder.Services.AddEventingForDbContext<IdentityModuleDbContext>();
        builder.Services.AddIntegrationEventHandlers(typeof(IdentityModule).Assembly);

        builder.Services.AddIntegrationEventType<UserCreatedIntegrationEvent>();
        builder.Services.AddIntegrationEventType<UserDeactivatedIntegrationEvent>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<IdentityModuleDbContext>("db:identity", tags: [HealthEndpoints.ReadyTag]);
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapModuleGroup(Tag);

        group.MapIssueTokenEndpoint();
        group.MapRefreshTokenEndpoint();
        group.MapGetCurrentUserEndpoint();

        // Literal segments before the "{id:guid}" routes, or they would be shadowed.
        group.MapSearchUsersEndpoint();
        group.MapCreateUserEndpoint();
        group.MapSetUserStatusEndpoint();
        group.MapAssignRolesEndpoint();
        group.MapGetUserEndpoint();

        group.MapListRolesEndpoint();
        group.MapCreateRoleEndpoint();
        group.MapUpdateRolePermissionsEndpoint();
    }
}
