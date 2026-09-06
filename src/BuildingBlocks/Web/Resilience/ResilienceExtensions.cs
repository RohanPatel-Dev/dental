using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dental.Framework.Web.Resilience;

/// <summary>
/// Opt-in resilience for outbound HTTP clients.
/// </summary>
/// <remarks>
/// Deliberately NOT applied globally: retrying every outbound call, including ones that are not
/// idempotent, turns one failure into several. For background work prefer Hangfire's
/// <c>[AutomaticRetry]</c>, which survives a process restart in a way an in-process pipeline cannot.
/// </remarks>
public static class ResilienceExtensions
{
    /// <summary>Adds the standard retry, timeout and circuit breaker pipeline to one client.</summary>
    /// <param name="builder">The client builder.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The client builder, for chaining.</returns>
    public static IHttpClientBuilder AddHeroResilience(
        this IHttpClientBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        ResilienceOptions options =
            configuration.GetSection(nameof(ResilienceOptions)).Get<ResilienceOptions>()
            ?? new ResilienceOptions();

        builder.AddStandardResilienceHandler(resilience =>
        {
            resilience.Retry.MaxRetryAttempts = options.MaxRetryAttempts;
            resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(options.TotalTimeoutSeconds);
            resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(options.AttemptTimeoutSeconds);
            resilience.CircuitBreaker.FailureRatio = options.CircuitBreakerFailureRatio;
            resilience.CircuitBreaker.SamplingDuration =
                TimeSpan.FromSeconds(Math.Max(options.AttemptTimeoutSeconds * 2, 30));
        });

        return builder;
    }
}

/// <summary>Resilience configuration, bound from the <c>ResilienceOptions</c> section.</summary>
public sealed class ResilienceOptions
{
    /// <summary>Retries after the first attempt.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Budget for all attempts combined, in seconds.</summary>
    public int TotalTimeoutSeconds { get; set; } = 30;

    /// <summary>Budget for one attempt, in seconds.</summary>
    public int AttemptTimeoutSeconds { get; set; } = 10;

    /// <summary>Failure ratio that trips the circuit breaker.</summary>
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;
}
