namespace Dental.Framework.Core.Contracts;

/// <summary>
/// Receives audit records produced by the persistence layer.
/// </summary>
/// <remarks>
/// Defined here, in the lowest building block, so the Persistence interceptor can emit records
/// without depending on the Auditing module. The Auditing module supplies the implementation - if
/// none is registered the interceptor does nothing, which is the correct behaviour for a host that
/// does not load that module.
/// </remarks>
public interface IAuditSink
{
    /// <summary>Persists a batch of audit records.</summary>
    /// <param name="records">Records produced by one unit of work.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the records are stored.</returns>
    Task WriteAsync(IReadOnlyCollection<AuditRecord> records, CancellationToken cancellationToken = default);
}

/// <summary>One change to one entity, as captured by the audit interceptor.</summary>
/// <param name="EntityName">CLR type name of the changed entity.</param>
/// <param name="EntityId">Primary key of the changed entity.</param>
/// <param name="Operation">Operation name from <c>AuditOperations</c>.</param>
/// <param name="Module">Assembly simple name of the module that owns the entity.</param>
/// <param name="TenantId">Tenant the change belongs to.</param>
/// <param name="UserId">User that made the change, when there was one.</param>
/// <param name="OccurredOnUtc">When the change was saved.</param>
/// <param name="CorrelationId">Correlation identifier of the originating request.</param>
/// <param name="ChangedColumns">Names of the columns that changed.</param>
/// <param name="OldValuesJson">Previous values, with sensitive properties redacted.</param>
/// <param name="NewValuesJson">New values, with sensitive properties redacted.</param>
public sealed record AuditRecord(
    string EntityName,
    string EntityId,
    string Operation,
    string Module,
    string? TenantId,
    Guid? UserId,
    DateTimeOffset OccurredOnUtc,
    string? CorrelationId,
    IReadOnlyList<string> ChangedColumns,
    string? OldValuesJson,
    string? NewValuesJson);
