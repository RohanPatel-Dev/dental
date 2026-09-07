using Dental.Framework.Persistence.Initialization;
using Dental.Framework.Shared.Tenancy;
using Dental.Framework.Web.Tenancy;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Dental.Framework.Web.Migrations;

/// <summary>
/// Applies migrations and runs seeders, tenant catalog first and then each tenant in turn.
/// </summary>
/// <remarks>
/// The whole run is wrapped in a Postgres ADVISORY LOCK. Two migrators started at once - a rolling
/// deploy, a retried CI job, an impatient operator - would otherwise race on the same DDL and leave
/// the schema half applied.
/// </remarks>
/// <param name="scopeFactory">Creates a scope per tenant.</param>
/// <param name="tenantStore">Enumerates the tenants to migrate.</param>
/// <param name="logger">Logger.</param>
public sealed class MigrationRunner(
    IServiceScopeFactory scopeFactory,
    IMultiTenantStore<DentalTenantInfo> tenantStore,
    ILogger<MigrationRunner> logger)
{
    /// <summary>Arbitrary but stable key identifying this application's migration lock.</summary>
    private const long AdvisoryLockKey = 0x44_45_4E_54_41_4C_01;

    /// <summary>Applies migrations, and optionally seeds, for the requested scope.</summary>
    /// <param name="options">What to run.</param>
    /// <param name="connectionString">Connection used to take the advisory lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the run is finished.</returns>
    public async Task RunAsync(
        MigrationRunOptions options,
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        NpgsqlConnection connection = new(connectionString);
        await using (connection.ConfigureAwait(false))
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await AcquireLockAsync(connection, cancellationToken).ConfigureAwait(false);

            try
            {
                await RunWithinLockAsync(options, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await ReleaseLockAsync(connection, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task RunWithinLockAsync(
        MigrationRunOptions options,
        CancellationToken cancellationToken)
    {
        // The catalog first: nothing else can be migrated until we know which tenants exist.
        logger.LogInformation("Migrating the tenant catalog.");
        await RunForTenantAsync(TenantConstants.RootTenant, options, cancellationToken)
            .ConfigureAwait(false);

        if (options.CatalogOnly)
        {
            logger.LogInformation("Catalog-only run requested; stopping here.");
            return;
        }

        IEnumerable<DentalTenantInfo> tenants = await tenantStore.GetAllAsync().ConfigureAwait(false);

        DentalTenantInfo[] targets =
        [
            .. tenants.Where(t =>
                options.TenantId is null
                || string.Equals(t.Identifier, options.TenantId, StringComparison.Ordinal)),
        ];

        if (options.TenantId is not null && targets.Length == 0)
        {
            throw new InvalidOperationException($"Tenant '{options.TenantId}' was not found.");
        }

        foreach (string identifier in targets.Select(t => t.Identifier))
        {
            logger.LogInformation("Migrating tenant {TenantId}.", identifier);
            await RunForTenantAsync(identifier, options, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Migrated {Count} tenant(s).", targets.Length);
    }

    private async Task RunForTenantAsync(
        string tenantId,
        MigrationRunOptions options,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        // Finbuckle's context is AsyncLocal, so it is set INLINE here rather than inside an awaited
        // helper that could return before the initializers run.
        IMultiTenantContextSetter setter =
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();

        setter.MultiTenantContext = new MultiTenantContext<DentalTenantInfo>(
            new DentalTenantInfo { Id = tenantId, Identifier = tenantId });

        IDbInitializer[] initializers = [.. scope.ServiceProvider.GetServices<IDbInitializer>()];

        foreach (IDbInitializer initializer in initializers)
        {
            await initializer.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }

        if (options.Seed)
        {
            foreach (IDbInitializer initializer in initializers)
            {
                await initializer.SeedAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        if (options.SeedDemo)
        {
            foreach (IDbInitializer initializer in initializers)
            {
                await initializer.SeedDemoAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task AcquireLockAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        logger.LogInformation("Waiting for the migration advisory lock.");

        NpgsqlCommand command = new("SELECT pg_advisory_lock(@key);", connection);
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.AddWithValue("key", AdvisoryLockKey);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Migration advisory lock acquired.");
    }

    private async Task ReleaseLockAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        NpgsqlCommand command = new("SELECT pg_advisory_unlock(@key);", connection);
        await using (command.ConfigureAwait(false))
        {
            command.Parameters.AddWithValue("key", AdvisoryLockKey);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Migration advisory lock released.");
    }
}

/// <summary>What one migrator run should do.</summary>
/// <param name="Seed">Run the required seeders.</param>
/// <param name="SeedDemo">Run the demo seeders. Never automatic.</param>
/// <param name="CatalogOnly">Stop after the tenant catalog.</param>
/// <param name="TenantId">Migrate only this tenant, when supplied.</param>
public sealed record MigrationRunOptions(
    bool Seed,
    bool SeedDemo,
    bool CatalogOnly,
    string? TenantId);
