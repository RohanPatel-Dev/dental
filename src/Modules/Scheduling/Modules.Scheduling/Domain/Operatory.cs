using Dental.Framework.Core.Domain;

namespace Dental.Modules.Scheduling.Domain;

/// <summary>A treatment room or chair that an appointment occupies.</summary>
public sealed class Operatory : BaseEntity, IAuditableEntity
{
    /// <summary>Room name, e.g. <c>Surgery 2</c>.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Whether it can be booked.</summary>
    public bool IsActive { get; set; } = true;

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }
}
