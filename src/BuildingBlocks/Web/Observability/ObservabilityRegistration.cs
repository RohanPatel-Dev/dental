using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using Dental.Framework.Caching;
using Dental.Framework.Shared.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace Dental.Framework.Web.Observability;

/// <summary>Wires Serilog and OpenTelemetry.</summary>
public static class ObservabilityRegistration
{
    /// <summary>Adds Serilog and, when enabled, OpenTelemetry traces and metrics.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <remarks>
    /// OTLP export is enabled by the config flag OR by the presence of
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c>, which Aspire injects. <c>service.name</c> prefers
    /// <c>OTEL_SERVICE_NAME</c> so the process adopts the orchestrator's identity instead of
    /// appearing twice under two names.
    /// </remarks>
    public static IHostApplicationBuilder AddHeroObservability(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ObservabilityOptions options =
            builder.Configuration.GetSection(nameof(ObservabilityOptions)).Get<ObservabilityOptions>()
            ?? new ObservabilityOptions();

        string? otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        bool exportOtlp = options.EnableOpenTelemetry || !string.IsNullOrWhiteSpace(otlpEndpoint);

        string serviceName = builder.Configuration["OTEL_SERVICE_NAME"]
            ?? builder.Environment.ApplicationName;

        ConfigureSerilog(builder, serviceName, exportOtlp, otlpEndpoint);

        if (!exportOtlp)
        {
            return builder;
        }

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(instrumentation =>
                    instrumentation.Filter = context => !IsNoise(context.Request.Path))
                .AddHttpClientInstrumentation()
                .AddSource("Npgsql")
                .AddSource(CacheTelemetry.Name)
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddNpgsqlInstrumentation()
                .AddMeter(CacheTelemetry.Name)
                .AddOtlpExporter());

        return builder;
    }

    private static void ConfigureSerilog(
        IHostApplicationBuilder builder,
        string serviceName,
        bool exportOtlp,
        string? otlpEndpoint)
    {
        builder.Services.AddSerilog((services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service.name", serviceName)

                // The global handler already logs the exception with full context; letting its own
                // source through as well double-logs every failed request.
                .Filter.ByExcluding(e => e.Properties.TryGetValue("SourceContext", out LogEventPropertyValue? source)
                    && source.ToString().Contains("GlobalExceptionHandler", StringComparison.Ordinal))

                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
                .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
                .MinimumLevel.Override("Finbuckle", LogEventLevel.Warning)
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);

            // Serilog owns the logging pipeline and does not forward to other ILogger providers, so
            // OTel log export has to be a Serilog sink rather than an OpenTelemetry logging builder.
            if (exportOtlp)
            {
                configuration.WriteTo.OpenTelemetry(sink =>
                {
                    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    {
                        sink.Endpoint = otlpEndpoint;
                    }

                    sink.ResourceAttributes = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["service.name"] = serviceName,
                    };
                });
            }
        });
    }

    private static bool IsNoise(PathString path) =>
        path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/alive", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/metrics", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Enriches every log event with request and, when authenticated, user properties.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder, for chaining.</returns>
    public static IApplicationBuilder UseHeroRequestLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseSerilogRequestLogging(logging =>
        {
            logging.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
                diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("TraceId", Activity.Current?.TraceId.ToString());

                if (httpContext.User.Identity?.IsAuthenticated != true)
                {
                    return;
                }

                diagnosticContext.Set("UserId", httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
                diagnosticContext.Set("UserEmail", httpContext.User.FindFirstValue(ClaimTypes.Email));
                diagnosticContext.Set("Tenant", httpContext.User.FindFirstValue(DentalClaims.Tenant));
            };
        });
    }
}

/// <summary>Observability configuration, bound from the <c>ObservabilityOptions</c> section.</summary>
public sealed class ObservabilityOptions
{
    /// <summary>
    /// Forces OpenTelemetry on. Export is also enabled automatically when
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is present, which is how Aspire turns it on.
    /// </summary>
    public bool EnableOpenTelemetry { get; set; }
}
