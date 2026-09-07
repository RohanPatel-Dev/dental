using Dental.Framework.Shared.Tenancy;
using Dental.Framework.Web.Tenancy;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dental.Framework.Web.Migrations;

/// <summary>Reports pending migrations per context without applying anything.</summary>
public static class PendingMigrationReporter
{
    /// <summary>Writes the pending migration list for every registered context.</summary>
    /// <param name="services">Root service provider.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes once the report is written.</returns>
    public static async Task ReportAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(logger);

        using IServiceScope scope = services.CreateScope();

        IMultiTenantContextSetter setter =
            scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();

        setter.MultiTenantContext = new MultiTenantContext<DentalTenantInfo>(
            new DentalTenantInfo
            {
                Id = TenantConstants.RootTenant,
                Identifier = TenantConstants.RootTenant,
            });

        DbContext[] contexts = [.. scope.ServiceProvider.GetServices<DbContext>()];

        foreach (DbContext context in contexts)
        {
            IEnumerable<string> pending = await context.Database
                .GetPendingMigrationsAsync(cancellationToken)
                .ConfigureAwait(false);

            string[] names = [.. pending];

            if (names.Length == 0)
            {
                logger.LogInformation("{Context}: up to date.", context.GetType().Name);
                continue;
            }

            logger.LogWarning(
                "{Context}: {Count} pending migration(s): {Migrations}",
                context.GetType().Name,
                names.Length,
                string.Join(", ", names));
        }
    }
}
