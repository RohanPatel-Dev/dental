using Dental.Framework.Core.Domain;

namespace Dental.Modules.Auditing.Domain;

/// <summary>
/// One recorded change, written by the persistence layer's audit interceptor.
/// </summary>
/// <remarks>
/// Append only: nothing in the application updates or deletes an audit row, and the retention job
/// is the only writer that removes them.
/// </remarks>
public sealed class AuditTrail : BaseEntity
{
    /// <summary>Entity type that changed.</summary>
    public string EntityName { get; set; } = default!;

    /// <summary>Primary key of the changed row, as text because keys are not all Guids.</summary>
    public string EntityId { get; set; } = default!;

    /// <summary>Operation name from <c>AuditOperations</c>.</summary>
    public string Operation { get; set; } = default!;

    /// <summary>Module that owns the entity.</summary>
    public string Module { get; set; } = default!;

    /// <summary>User that made the change, when there was one.</summary>
    public Guid? UserId { get; set; }

    /// <summary>When the change was saved.</summary>
    public DateTimeOffset OccurredOnUtc { get; set; }

    /// <summary>Correlation identifier of the originating request.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Comma separated list of the columns that changed.</summary>
    public string ChangedColumns { get; set; } = string.Empty;

    /// <summary>Previous values as JSON, with sensitive properties already redacted.</summary>
    public string? OldValues { get; set; }

    /// <summary>New values as JSON, with sensitive properties already redacted.</summary>
    public string? NewValues { get; set; }
}
