using Dental.Framework.Core.Domain;

namespace Dental.Modules.Scheduling.Domain;

/// <summary>A clinician who sees patients.</summary>
public sealed class Provider : BaseEntity, IAuditableEntity, ISoftDeletable
{
    /// <summary>Linked staff account, when there is one.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Name shown on the appointment book.</summary>
    public string DisplayName { get; set; } = default!;

    /// <summary>Their speciality, e.g. <c>Orthodontics</c>.</summary>
    public string? Speciality { get; set; }

    /// <summary>Whether new patients may be booked with them.</summary>
    public bool IsAcceptingPatients { get; set; } = true;

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; set; }
}
