using Dental.Framework.Persistence.Initialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Notifications.Data;

/// <summary>Migrates the notifications schema. Nothing to seed.</summary>
/// <param name="context">The notifications context.</param>
/// <param name="logger">Logger.</param>
public sealed class NotificationsDbInitializer(
    NotificationsDbContext context,
    ILogger<NotificationsDbInitializer> logger) : IDbInitializer
{
    /// <inheritdoc />
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false))
            .Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Applied pending migrations for the notifications schema.");
        }
    }

    /// <inheritdoc />
    public Task SeedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
