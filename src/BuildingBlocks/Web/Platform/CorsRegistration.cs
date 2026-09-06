using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dental.Framework.Web.Platform;

/// <summary>CORS policy configuration.</summary>
public static class CorsRegistration
{
    /// <summary>Name of the single policy the platform applies.</summary>
    public const string PolicyName = "hero";

    /// <summary>Registers the CORS policy.</summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// When allowing every origin we use <c>SetIsOriginAllowed(_ =&gt; true)</c>, NEVER
    /// <c>AllowAnyOrigin()</c>. <c>Access-Control-Allow-Origin: *</c> is illegal on a credentialed
    /// request, and SignalR's negotiate is always credentialed - so <c>AllowAnyOrigin()</c> breaks
    /// realtime while REST keeps working, which is close to undiagnosable from the symptom.
    /// </remarks>
    public static IServiceCollection AddHeroCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        CorsOptions options = configuration.GetSection(nameof(CorsOptions)).Get<CorsOptions>()
            ?? new CorsOptions();

        services.AddCors(cors => cors.AddPolicy(PolicyName, policy =>
        {
            if (options.AllowedOrigins.Count == 0)
            {
                policy.SetIsOriginAllowed(_ => true);
            }
            else
            {
                policy.WithOrigins([.. options.AllowedOrigins]);
            }

            policy.AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .WithExposedHeaders(
                    Shared.Http.HeaderNames.CorrelationId,
                    Shared.Http.HeaderNames.IdempotencyReplayed,
                    Shared.Http.HeaderNames.RetryAfter);
        }));

        return services;
    }
}

/// <summary>CORS configuration, bound from the <c>CorsOptions</c> section.</summary>
public sealed class CorsOptions
{
    /// <summary>Allowed origins. Empty means "reflect any origin", which is fine for development.</summary>
    public IList<string> AllowedOrigins { get; } = [];
}
