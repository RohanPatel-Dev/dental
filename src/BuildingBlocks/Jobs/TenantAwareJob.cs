using Dental.Framework.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Dental.Framework.Jobs;

/// <summary>
/// Base for jobs that touch tenant data.
/// </summary>
/// <remarks>
/// A Hangfire job runs with no HTTP context and therefore no ambient tenant. This base creates a
/// fresh scope, restores the tenant, and hands the scoped provider to the derived job - which is
/// exactly the dance every tenant aware job would otherwise have to repeat.
/// </remarks>
/// <param name="scopeFactory">Creates the per-run scope.</param>
public abstract class TenantAwareJob(IServiceScopeFactory scopeFactory)
{
    /// <summary>Runs the job body inside a scope bound to one tenant.</summary>
    /// <param name="tenantId">Tenant to run for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the body has run.</returns>
    protected async Task RunForTenantAsync(string tenantId, CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        ITenantContextRestorer restorer =
            scope.ServiceProvider.GetRequiredService<ITenantContextRestorer>();

        await restorer.RestoreAsync(tenantId, cancellationToken).ConfigureAwait(false);
        await ExecuteAsync(scope.ServiceProvider, tenantId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The job body, with the tenant already restored.</summary>
    /// <param name="services">Scoped service provider.</param>
    /// <param name="tenantId">Tenant the run is for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the work is done.</returns>
    protected abstract Task ExecuteAsync(
        IServiceProvider services,
        string tenantId,
        CancellationToken cancellationToken);
}
