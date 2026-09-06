namespace Dental.Modules.Auditing.Contracts.Dtos;

/// <summary>One recorded change.</summary>
/// <param name="Id">Audit row identifier.</param>
/// <param name="EntityName">Entity type that changed.</param>
/// <param name="EntityId">Primary key of the changed row.</param>
/// <param name="Operation">Operation name.</param>
/// <param name="Module">Module that owns the entity.</param>
/// <param name="UserId">User that made the change.</param>
/// <param name="OccurredOnUtc">When the change was saved.</param>
/// <param name="CorrelationId">Correlation identifier of the originating request.</param>
/// <param name="ChangedColumns">Columns that changed.</param>
/// <param name="OldValues">Previous values, with sensitive properties redacted.</param>
/// <param name="NewValues">New values, with sensitive properties redacted.</param>
public sealed record AuditTrailDto(
    Guid Id,
    string EntityName,
    string EntityId,
    string Operation,
    string Module,
    Guid? UserId,
    DateTimeOffset OccurredOnUtc,
    string? CorrelationId,
    IReadOnlyList<string> ChangedColumns,
    string? OldValues,
    string? NewValues);
