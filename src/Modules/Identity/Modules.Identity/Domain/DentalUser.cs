using Dental.Framework.Core.Domain;
using Microsoft.AspNetCore.Identity;

namespace Dental.Modules.Identity.Domain;

/// <summary>A staff account. Tenant scoped: the same email may exist in two practices.</summary>
public sealed class DentalUser : IdentityUser<Guid>, IHasTenant, IAuditableEntity, ISoftDeletable
{
    /// <summary>Given name.</summary>
    public string FirstName { get; set; } = default!;

    /// <summary>Family name.</summary>
    public string LastName { get; set; } = default!;

    /// <summary>Whether the account may sign in.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Object key of the profile image, when one has been uploaded.</summary>
    public string? AvatarKey { get; set; }

    /// <summary>When the account last signed in successfully.</summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <inheritdoc />
    public string TenantId { get; set; } = default!;

    /// <summary>When the account was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the account was last modified.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; set; }

    /// <summary>Display name.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}
