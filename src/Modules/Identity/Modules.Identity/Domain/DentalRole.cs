using Dental.Framework.Core.Domain;
using Microsoft.AspNetCore.Identity;

namespace Dental.Modules.Identity.Domain;

/// <summary>A role. Tenant scoped, so each practice can define its own beyond the built-in four.</summary>
public sealed class DentalRole : IdentityRole<Guid>, IHasTenant, IAuditableEntity
{
    /// <summary>What the role is for.</summary>
    public string? Description { get; set; }

    /// <summary>Built in roles are seeded per tenant and cannot be deleted.</summary>
    public bool IsBuiltIn { get; set; }

    /// <inheritdoc />
    public string TenantId { get; set; } = default!;

    /// <summary>When the role was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the role was last modified.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }
}
