using Dental.Framework.Persistence.Initialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Billing.Data;

/// <summary>Migrates the billing schema. The ledger starts empty by design.</summary>
/// <param name="context">The billing context.</param>
/// <param name="logger">Logger.</param>
public sealed class BillingDbInitializer(
    BillingDbContext context,
    ILogger<BillingDbInitializer> logger) : IDbInitializer
{
    /// <inheritdoc />
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if ((await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false))
            .Any())
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Applied pending migrations for the billing schema.");
        }
    }

    /// <inheritdoc />
    public Task SeedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
