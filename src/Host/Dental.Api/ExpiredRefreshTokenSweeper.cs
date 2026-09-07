using Dental.Modules.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace Dental.Api;

/// <summary>
/// Deletes refresh tokens that expired or were consumed long enough ago to be of no forensic value.
/// </summary>
/// <remarks>
/// Registered by the HOST, not by the Identity module. It is once-per-process work: a module level
/// registration would give every host that loads Identity its own copy of the same sweep, all
/// competing over the same rows.
/// </remarks>
/// <param name="scopeFactory">Creates a scope per sweep.</param>
/// <param name="timeProvider">Clock.</param>
/// <param name="logger">Logger.</param>
public sealed class ExpiredRefreshTokenSweeper(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<ExpiredRefreshTokenSweeper> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await SweepAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Refresh token sweep failed; the loop continues.");
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IdentityModuleDbContext context =
            scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();

        DateTimeOffset cutoff = timeProvider.GetUtcNow() - Retention;

        // Cross-tenant on purpose: this is housekeeping the operator owns, and the sweep runs with
        // no tenant context. The predicate is a date, so nothing tenant-specific leaks.
        int deleted = await context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (deleted > 0)
        {
            logger.LogInformation("Swept {Count} expired refresh token(s).", deleted);
        }
    }
}
