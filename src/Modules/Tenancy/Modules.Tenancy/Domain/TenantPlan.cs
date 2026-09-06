using Dental.Framework.Core.Domain;

namespace Dental.Modules.Tenancy.Domain;

/// <summary>
/// A subscription plan and the quota limits it grants. Global reference data shared by every tenant.
/// </summary>
public sealed class TenantPlan : BaseEntity, IGlobalEntity
{
    /// <summary>Plan name, unique.</summary>
    public string Name { get; set; } = default!;

    /// <summary>What the plan includes.</summary>
    public string Description { get; set; } = default!;

    /// <summary>Requests per quota window.</summary>
    public long ApiCallLimit { get; set; }

    /// <summary>Bytes of object storage.</summary>
    public long StorageByteLimit { get; set; }

    /// <summary>Active users.</summary>
    public long UserLimit { get; set; }

    /// <summary>Patient records.</summary>
    public long PatientLimit { get; set; }

    /// <summary>Outbound messages per quota window.</summary>
    public long NotificationLimit { get; set; }
}
