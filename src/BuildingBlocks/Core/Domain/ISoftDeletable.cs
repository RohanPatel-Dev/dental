namespace Dental.Framework.Core.Domain;

/// <summary>Rows implementing this are never physically deleted; the interceptor flips the flags.</summary>
public interface ISoftDeletable
{
    /// <summary>When the row was soft deleted, or <see langword="null"/> while it is live.</summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>User that soft deleted the row.</summary>
    Guid? DeletedBy { get; set; }
}
