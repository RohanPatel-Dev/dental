using Dental.Modules.Auditing.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dental.Modules.Auditing.Jobs;

/// <summary>Deletes audit rows older than the configured retention window.</summary>
/// <remarks>
/// Registered as a recurring job from <c>AuditingModule.MapEndpoints</c>, because
/// <c>IJobService</c> deliberately has no recurring-job API.
/// </remarks>
/// <param name="context">The auditing context.</param>
/// <param name="options">Retention configuration.</param>
/// <param name="timeProvider">Clock.</param>
/// <param name="logger">Logger.</param>
public sealed class AuditRetentionJob(
    AuditingDbContext context,
    IOptions<AuditingOptions> options,
    TimeProvider timeProvider,
    ILogger<AuditRetentionJob> logger)
{
    /// <summary>Deletes rows past their retention date across every tenant.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the sweep is done.</returns>
    [AutomaticRetry(Attempts = 2)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = timeProvider.GetUtcNow().AddDays(-options.Value.RetentionDays);

        // Deliberately cross-tenant: retention is an operator obligation, not a tenant feature, and
        // the job runs with no tenant context. The filter is dropped explicitly and the predicate
        // is a date, not a tenant, so nothing leaks.
        int deleted = await context.AuditTrails
            .IgnoreQueryFilters()
            .Where(a => a.OccurredOnUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        if (deleted > 0)
        {
            logger.LogInformation(
                "Deleted {Count} audit record(s) older than {Cutoff}.",
                deleted,
                cutoff);
        }
    }
}
