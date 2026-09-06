using System.Text;
using Asp.Versioning;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Dental.Framework.Caching;
using Dental.Framework.Core.Contracts;
using Dental.Framework.Jobs;
using Dental.Framework.Mailing;
using Dental.Framework.Persistence.Extensions;
using Dental.Framework.Quota;
using Dental.Framework.Shared.Identity;
using Dental.Framework.Shared.Tenancy;
using Dental.Framework.Storage;
using Dental.Framework.Web.Auth;
using Dental.Framework.Web.Exceptions;
using Dental.Framework.Web.Observability;
using Dental.Framework.Web.RateLimiting;
using Dental.Framework.Web.Realtime;
using Dental.Framework.Web.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace Dental.Framework.Web.Platform;

/// <summary>One call that registers the entire HTTP platform.</summary>
public static class PlatformRegistration
{
    /// <summary>
    /// Registers authentication, authorization, CORS, versioning, OpenAPI, health checks,
    /// observability, and every subsystem enabled by <paramref name="configure"/>.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Feature flags.</param>
    /// <returns>The builder, for chaining.</returns>
    public static IHostApplicationBuilder AddHeroPlatform(
        this IHostApplicationBuilder builder,
        Action<HeroPlatformOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        HeroPlatformOptions options = new();
        configure?.Invoke(options);

        builder.AddHeroObservability();

        builder.Services.AddHttpContextAccessor();
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddScoped<ICurrentUser, CurrentUser>();
        builder.Services.TryAddScoped<IRequestContext, RequestContext>();
        builder.Services.TryAddScoped<ITenantContextRestorer, TenantContextRestorer>();

        builder.Services.AddHeroPersistence(builder.Configuration);
        builder.Services.AddHeroCors(builder.Configuration);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        AddMultiTenancy(builder);
        AddAuth(builder);
        AddVersioningAndOpenApi(builder);

        builder.Services.AddHealthChecks();

        if (options.EnableCaching)
        {
            builder.Services.AddHeroCaching(builder.Configuration);
            AddDataProtection(builder);
        }

        if (options.EnableStorage)
        {
            builder.Services.AddHeroStorage(builder.Configuration);
        }

        if (options.EnableMailing)
        {
            builder.Services.AddHeroMailing();
        }

        if (options.EnableJobs)
        {
            builder.Services.AddHeroJobs(builder.Configuration);
        }

        if (options.EnableQuotas)
        {
            builder.Services.AddHeroQuotas(builder.Configuration);
        }

        if (options.EnableRateLimiting)
        {
            builder.Services.AddHeroRateLimiting(builder.Configuration);
        }

        if (options.EnableRealtime)
        {
            AddRealtime(builder);
        }

        if (options.EnableSse)
        {
            builder.Services.TryAddSingleton<SseConnectionManager>();
            builder.Services.TryAddSingleton<SseTokenStore>();
        }

        return builder;
    }

    private static void AddMultiTenancy(IHostApplicationBuilder builder)
    {
        // Resolution runs BEFORE authentication, so it is header driven. The claim strategy is a
        // fallback for callers that authenticate without stating a tenant.
        builder.Services.AddMultiTenant<DentalTenantInfo>()
            .WithHeaderStrategy(TenantConstants.Header)
            .WithClaimStrategy(TenantConstants.ClaimType)
            .WithStaticStrategy(TenantConstants.RootTenant);
    }

    private static void AddAuth(IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<JwtOptions>()
            .BindConfiguration(nameof(JwtOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        JwtOptions jwt = builder.Configuration.GetSection(nameof(JwtOptions)).Get<JwtOptions>()
            ?? new JwtOptions();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearer =>
            {
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            string.IsNullOrWhiteSpace(jwt.SigningKey)
                                ? new string('0', 32)
                                : jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
                };

                bearer.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // SignalR cannot set an Authorization header on the WebSocket upgrade, so it
                        // passes the token in the query string instead.
                        string? accessToken = context.Request.Query["access_token"];
                        PathString path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken)
                            && path.StartsWithSegments(
                                Shared.Http.ApiRoutes.RealtimeHub,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        builder.Services.AddSingleton<
            Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
            PermissionAuthorizationHandler>();

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Permission, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement()))
            .SetDefaultPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement())
                .Build());
    }

    private static void AddVersioningAndOpenApi(IHostApplicationBuilder builder)
    {
        builder.Services.AddApiVersioning(versioning =>
        {
            versioning.DefaultApiVersion = new ApiVersion(1);
            versioning.AssumeDefaultVersionWhenUnspecified = true;
            versioning.ReportApiVersions = true;
            versioning.ApiVersionReader = new UrlSegmentApiVersionReader();
        });

        builder.Services.AddOpenApi();
    }

    private static void AddRealtime(IHostApplicationBuilder builder)
    {
        var signalR = builder.Services.AddSignalR(hub =>
        {
            hub.EnableDetailedErrors = builder.Environment.IsDevelopment();
        });

        string? redis = builder.Configuration[$"{nameof(CachingOptions)}:{nameof(CachingOptions.Redis)}"];
        if (!string.IsNullOrWhiteSpace(redis))
        {
            // Required for more than one replica: without a backplane, a message published on node A
            // never reaches a browser connected to node B.
            signalR.AddStackExchangeRedis(redis);
        }

        builder.Services.TryAddSingleton<IRealtimeNotifier, RealtimeNotifier>();
    }

    private static void AddDataProtection(IHostApplicationBuilder builder)
    {
        string? redis = builder.Configuration[$"{nameof(CachingOptions)}:{nameof(CachingOptions.Redis)}"];
        if (string.IsNullOrWhiteSpace(redis))
        {
            return;
        }

        builder.Services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(
                new Lazy<IConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(redis)).Value,
                "dental:dataprotection-keys");
    }

    /// <summary>
    /// Fails fast when production configuration is missing, BEFORE any service registration.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <exception cref="InvalidOperationException">A required production setting is absent.</exception>
    /// <remarks>
    /// Starting with a missing signing key or connection string produces a process that accepts
    /// traffic and fails every request. Refusing to start is the kinder failure.
    /// </remarks>
    public static void ValidateProductionConfiguration(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!builder.Environment.IsProduction())
        {
            return;
        }

        string[] required =
        [
            "DatabaseOptions:ConnectionString",
            "CachingOptions:Redis",
            "JwtOptions:SigningKey",
        ];

        List<string> missing = [.. required.Where(k => string.IsNullOrWhiteSpace(builder.Configuration[k]))];

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Refusing to start in Production: the following configuration values are missing: "
                + string.Join(", ", missing));
        }

        _ = PermissionConstants.All;
    }
}
