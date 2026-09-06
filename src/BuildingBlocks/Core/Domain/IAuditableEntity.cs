namespace Dental.Framework.Core.Domain;

/// <summary>Populated by the audit interceptor on every save.</summary>
public interface IAuditableEntity
{
    /// <summary>User that created the row.</summary>
    Guid? CreatedBy { get; set; }

    /// <summary>User that last modified the row.</summary>
    Guid? UpdatedBy { get; set; }
}
