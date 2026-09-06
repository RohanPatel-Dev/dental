namespace Dental.Framework.Persistence.Initialization;

/// <summary>
/// Per-module migrate and seed hook, run by the one-shot DbMigrator host - never at API startup.
/// </summary>
public interface IDbInitializer
{
    /// <summary>Applies this module's pending migrations for the current tenant.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when migration is done.</returns>
    Task MigrateAsync(CancellationToken cancellationToken = default);

    /// <summary>Seeds the data this module cannot run without.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding is done.</returns>
    Task SeedAsync(CancellationToken cancellationToken = default);

    /// <summary>Seeds illustrative demo data. Never invoked automatically.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding is done.</returns>
    Task SeedDemoAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
