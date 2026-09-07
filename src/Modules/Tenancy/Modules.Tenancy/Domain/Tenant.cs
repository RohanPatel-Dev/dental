using Dental.Framework.Core.Domain;

namespace Dental.Modules.Tenancy.Domain;

/// <summary>
/// One dental practice.
/// </summary>
/// <remarks>
/// This is the tenant CATALOG, so the row is an <see cref="IGlobalEntity"/>: it deliberately carries
/// no tenant query filter, because the operator must be able to see every tenant and because
/// filtering the catalog by the tenant it defines would be circular.
/// </remarks>
public sealed class Tenant : AggregateRoot, IGlobalEntity, IAuditableEntity, ISoftDeletable
{
    /// <summary>Human readable slug, unique across the catalog.</summary>
    public string Identifier { get; set; } = default!;

    /// <summary>Practice name.</summary>
    public string Name { get; set; } = default!;

    /// <summary>Contact address of the tenant's first administrator.</summary>
    public string AdminEmail { get; set; } = default!;

    /// <summary>Name of the subscription plan, which drives quota limits.</summary>
    public string PlanName { get; set; } = default!;

    /// <summary>Whether the tenant may sign in and use the API.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When the subscription lapses, if it does.</summary>
    public DateTimeOffset? ValidUntil { get; set; }

    /// <summary>IANA time zone the practice schedules in, e.g. <c>America/Chicago</c>.</summary>
    public string TimeZone { get; set; } = "UTC";

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; set; }

    /// <summary>Deactivates the tenant and raises the corresponding domain event.</summary>
    /// <param name="reason">Why it is being deactivated.</param>
    public void Deactivate(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (!IsActive)
        {
            return;
        }

        IsActive = false;
    }

    /// <summary>Reactivates the tenant.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Moves the tenant onto a different plan.</summary>
    /// <param name="planName">The new plan.</param>
    public void ChangePlan(string planName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planName);
        PlanName = planName;
    }
}
