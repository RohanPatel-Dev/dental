namespace Dental.Framework.Core.Domain;

/// <summary>
/// Marks an entity as belonging to exactly one tenant. Every entity is tenant scoped by default;
/// the only way out is <see cref="IGlobalEntity"/>.
/// </summary>
public interface IHasTenant
{
    /// <summary>Identifier of the owning tenant.</summary>
    string TenantId { get; set; }
}
