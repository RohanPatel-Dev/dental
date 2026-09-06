using Dental.Framework.Persistence.Initialization;
using Dental.Modules.Auditing.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Auditing.Data;

/// <summary>Migrates the auditing schema. There is nothing to seed - the trail starts empty.</summary>
/// <param name="context">The auditing context.</param>
/// <param name="logger">Logger.</param>
public sealed class AuditingDbInitializer(
    AuditingDbContext context,
    ILogger<AuditingDbInitializer> logger) : IDbInitializer
{
    /// <inheritdoc />
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false))
            .Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Applied pending migrations for the auditing schema.");
        }
    }

    /// <inheritdoc />
    public Task SeedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
