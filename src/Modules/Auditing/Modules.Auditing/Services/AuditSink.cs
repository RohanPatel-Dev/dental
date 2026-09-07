using Dental.Framework.Core.Contracts;
using Dental.Modules.Auditing.Data;
using Dental.Modules.Auditing.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dental.Modules.Auditing.Services;

/// <summary>
/// Persists audit records produced by the persistence layer's interceptor.
/// </summary>
/// <remarks>
/// Writes go through a FRESH scope with its own <c>AuditingDbContext</c>. The interceptor fires
/// inside another context's SaveChanges, and reusing that context would either re-enter SaveChanges
/// or bind the audit row to a transaction the trail should outlive.
/// </remarks>
/// <param name="scopeFactory">Creates the writing scope.</param>
/// <param name="logger">Logger.</param>
public sealed class AuditSink(IServiceScopeFactory scopeFactory, ILogger<AuditSink> logger) : IAuditSink
{
    /// <inheritdoc />
    public async Task WriteAsync(
        IReadOnlyCollection<AuditRecord> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (records.Count == 0)
        {
            return;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        AuditingDbContext context = scope.ServiceProvider.GetRequiredService<AuditingDbContext>();

        // The trail belongs to the tenant that MADE the change, which is not always the tenant the
        // changed row names: the operator tenant creating a practice writes a Tenant row stamped
        // with the new practice, and stamping the audit row the same way would make it a write
        // across a tenant boundary - which the context refuses outright.
        string? actingTenantId = context.TenantInfo?.Id;

        foreach (AuditRecord record in records)
        {
            context.AuditTrails.Add(new AuditTrail
            {
                EntityName = record.EntityName,
                EntityId = record.EntityId,
                Operation = record.Operation,
                Module = record.Module,
                UserId = record.UserId,
                OccurredOnUtc = record.OccurredOnUtc,
                CorrelationId = record.CorrelationId,
                ChangedColumns = string.Join(',', record.ChangedColumns),
                OldValues = record.OldValuesJson,
                NewValues = record.NewValuesJson,
                TenantId = actingTenantId ?? record.TenantId ?? string.Empty,
            });
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Wrote {Count} audit record(s).", records.Count);
    }
}
