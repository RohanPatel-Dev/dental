using Dental.Framework.Eventing.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dental.Framework.Eventing.Hosting;

/// <summary>
/// Polls every registered <see cref="IOutboxSweeper"/> and publishes pending messages.
/// </summary>
/// <param name="scopeFactory">Creates a scope per tick, since sweepers are scoped.</param>
/// <param name="options">Eventing options.</param>
/// <param name="timeProvider">Clock, so the interval is testable.</param>
/// <param name="logger">Logger.</param>
public sealed class OutboxDispatcherHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<EventingOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcherHostedService> logger) : BackgroundService
{
    private readonly EventingOptions _options = options.Value;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromSeconds(_options.OutboxDispatchIntervalSeconds);
        using PeriodicTimer timer = new(interval, timeProvider);

        logger.LogInformation(
            "Outbox dispatcher started with a {IntervalSeconds}s interval and batch size {BatchSize}.",
            _options.OutboxDispatchIntervalSeconds,
            _options.OutboxBatchSize);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            // A background loop must survive a bad tick, but it must never swallow silently:
            // log with context, and let cancellation through untouched.
            try
            {
                await SweepAllAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Outbox dispatch tick failed; the loop continues.");
            }
        }
    }

    private async Task SweepAllAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        foreach (IOutboxSweeper sweeper in scope.ServiceProvider.GetServices<IOutboxSweeper>())
        {
            int published = await sweeper.SweepAsync(cancellationToken).ConfigureAwait(false);
            if (published > 0)
            {
                logger.LogInformation(
                    "Published {Count} outbox message(s) for {Module}.",
                    published,
                    sweeper.OwnerAssembly);
            }
        }
    }
}
