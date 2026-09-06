using Dental.Framework.Jobs;
using Dental.Framework.Shared.Http;
using Dental.Framework.Web.Health;
using Dental.Framework.Web.Middleware;
using Dental.Framework.Web.Modules;
using Dental.Framework.Web.Observability;
using Dental.Framework.Web.Realtime;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Dental.Framework.Web.Platform;

/// <summary>One call that builds the entire HTTP pipeline, in the one order that works.</summary>
public static class PipelineRegistration
{
    /// <summary>
    /// Resolves the tenant. Must run BEFORE <see cref="UseHeroPlatform"/>, and therefore before
    /// authentication - which is why tenant resolution is header driven rather than claim driven.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder, for chaining.</returns>
    public static IApplicationBuilder UseHeroMultiTenantDatabases(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMultiTenant();
    }

    /// <summary>
    /// Builds the pipeline.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="configure">Feature flags.</param>
    /// <returns>The application, for chaining.</returns>
    /// <remarks>
    /// <para>The order below is load bearing:</para>
    /// <list type="number">
    /// <item>Exception handler, then response compression.</item>
    /// <item>
    /// CORS BEFORE HTTPS redirection. The other way round, an OPTIONS preflight gets a 307 and the
    /// browser refuses to follow it, so every cross-origin request fails with no server side error.
    /// </item>
    /// <item>Security headers, static files, routing.</item>
    /// <item><c>UseAuthentication</c>.</item>
    /// <item>Module middleware - after authentication, so the caller is known.</item>
    /// <item>Rate limiting, then quotas (which need a tenant), then authorization, then endpoints.</item>
    /// </list>
    /// <para>
    /// Anything that must rewrite <c>Request.Path</c> has to run BEFORE this call: routing happens
    /// inside, and once an endpoint is selected a rewrite is a no-op.
    /// </para>
    /// </remarks>
    public static WebApplication UseHeroPlatform(
        this WebApplication app,
        Action<HeroPipelineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        HeroPipelineOptions options = new();
        configure?.Invoke(options);

        app.UseExceptionHandler();
        app.UseResponseCompression();

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseHeroRequestLogging();

        // CORS BEFORE the HTTPS redirect - see the remarks above.
        app.UseCors(CorsRegistration.PolicyName);

        if (options.UseHttpsRedirection)
        {
            app.UseHttpsRedirection();
        }

        app.UseMiddleware<SecurityHeadersMiddleware>();

        if (options.UseStaticFiles)
        {
            app.UseStaticFiles();
        }

        app.UseRouting();

        app.UseAuthentication();

        // Module middleware runs here: after authentication, before authorization.
        app.UseModuleMiddlewares();

        if (app.Configuration.GetValue("RateLimitingOptions:Enabled", defaultValue: true))
        {
            app.UseRateLimiter();
        }

        if (app.Services.GetService<Framework.Quota.IQuotaService>() is not null)
        {
            app.UseMiddleware<QuotaEnforcementMiddleware>();
        }

        if (app.Services.GetService<Microsoft.Extensions.Caching.Hybrid.HybridCache>() is not null)
        {
            app.UseMiddleware<IdempotencyMiddleware>();
        }

        app.UseAuthorization();

        app.MapHeroHealthChecks();
        app.MapModules();

        if (options.MapOpenApi && !app.Environment.IsProduction())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(scalar => scalar.WithTitle("Dental API"));
        }

        if (options.MapRealtime && app.Services.GetService<IRealtimeNotifier>() is not null)
        {
            app.MapHub<AppHub>(ApiRoutes.RealtimeHub);
        }

        if (options.MapSse && app.Services.GetService<SseTokenStore>() is not null)
        {
            app.MapServerSentEvents();
        }

        if (options.MapJobsDashboard && app.Services.GetService<IJobService>() is HangfireJobService)
        {
            MapJobsDashboard(app);
        }

        return app;
    }

    private static void MapJobsDashboard(WebApplication app)
    {
        JobOptions jobOptions =
            app.Configuration.GetSection(nameof(JobOptions)).Get<JobOptions>() ?? new JobOptions();

        app.UseHangfireDashboard(jobOptions.DashboardPath, new DashboardOptions
        {
            Authorization =
            [
                new JobDashboardAuthorizationFilter(
                    Microsoft.Extensions.Options.Options.Create(jobOptions)),
            ],
        });
    }

    /// <summary>Maps a module's endpoints under the versioned prefix with a tag.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="tag">OpenAPI tag, normally the module name.</param>
    /// <returns>The group, ready for the module's endpoint registrations.</returns>
    public static RouteGroupBuilder MapModuleGroup(this IEndpointRouteBuilder endpoints, string tag)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        Asp.Versioning.Builder.ApiVersionSet versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1))
            .ReportApiVersions()
            .Build();

        return endpoints.MapGroup(ApiRoutes.VersionedPrefix)
            .WithTags(tag)
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();
    }
}
