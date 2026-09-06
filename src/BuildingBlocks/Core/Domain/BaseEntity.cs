namespace Dental.Framework.Core.Domain;

/// <summary>
/// Root of every persisted entity. Tenant scoped by default - implement <see cref="IGlobalEntity"/>
/// on the derived type to opt out.
/// </summary>
public abstract class BaseEntity : IHasTenant
{
    /// <summary>Primary key. UUIDv7 so it sorts by creation time and indexes well.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Creation timestamp, set by the audit interceptor.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last modification timestamp, set by the audit interceptor.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Owning tenant. Populated automatically by Finbuckle / the tenant interceptor.</summary>
    public string TenantId { get; set; } = default!;
}
