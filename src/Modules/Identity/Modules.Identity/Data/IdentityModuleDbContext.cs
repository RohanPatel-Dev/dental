using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Contexts;
using Dental.Modules.Identity.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Finbuckle.MultiTenant.EntityFrameworkCore.Extensions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Identity.Data;

/// <summary>
/// The Identity store.
/// </summary>
/// <remarks>
/// This is the one context that cannot derive from <see cref="BaseDbContext"/>: ASP.NET Identity's
/// stores require a context derived from <see cref="IdentityDbContext{TUser,TRole,TKey}"/>, and C#
/// has no multiple inheritance. It therefore implements <see cref="IMultiTenantDbContext"/> by hand
/// and applies exactly the same conventions through
/// <see cref="ModelConventions.ApplyDentalConventions"/>, followed by an explicit
/// <c>ConfigureMultiTenant()</c> - the two steps <c>MultiTenantDbContext</c> would otherwise do.
/// </remarks>
/// <param name="multiTenantContextAccessor">Finbuckle accessor.</param>
/// <param name="options">EF options.</param>
public sealed class IdentityModuleDbContext(
    IMultiTenantContextAccessor multiTenantContextAccessor,
    DbContextOptions<IdentityModuleDbContext> options)
    : IdentityDbContext<DentalUser, DentalRole, Guid>(options), IMultiTenantDbContext
{
    /// <summary>Schema name for this module.</summary>
    public const string SchemaName = "identity";

    /// <summary>Issued refresh tokens.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <inheritdoc />
    public ITenantInfo? TenantInfo => multiTenantContextAccessor.MultiTenantContext?.TenantInfo;

    /// <inheritdoc />
    public TenantMismatchMode TenantMismatchMode => TenantMismatchMode.Throw;

    /// <inheritdoc />
    public TenantNotSetMode TenantNotSetMode => TenantNotSetMode.Throw;

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        this.EnforceMultiTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Identity's own mappings first, then ours on top of the resulting model.
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(SchemaName);
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityModuleDbContext).Assembly);
        builder.ApplyEventingModel();

        builder.ApplyDentalConventions();

        MakeIdentityIndexesTenantScoped(builder);

        // LAST: installs Finbuckle's anonymous tenant filter over everything marked above.
        builder.ConfigureMultiTenant();
    }

    /// <summary>
    /// Drops the global unique indexes ASP.NET Identity declares on its own.
    /// </summary>
    /// <remarks>
    /// Identity makes <c>RoleNameIndex</c> and <c>UserNameIndex</c> unique on the normalized name
    /// ALONE. In a shared-schema multi-tenant store that means the second practice provisioned
    /// cannot have a role called "Admin", and no two practices can employ the same email address -
    /// the insert fails on a unique violation that mentions nothing about tenancy. The tenant
    /// scoped replacements already exist in <c>IdentityConfigurations</c>
    /// (<c>ux_roles_tenant_name</c> and <c>ux_users_tenant_email</c>); these two just have to go.
    /// </remarks>
    /// <param name="builder">The model builder.</param>
    private static void MakeIdentityIndexesTenantScoped(ModelBuilder builder)
    {
        RemoveIndex<DentalRole>(builder, nameof(DentalRole.NormalizedName));
        RemoveIndex<DentalUser>(builder, nameof(DentalUser.NormalizedUserName));
    }

    private static void RemoveIndex<TEntity>(ModelBuilder builder, string propertyName)
        where TEntity : class
    {
        Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entity =
            builder.Entity<TEntity>().Metadata;

        if (entity.FindIndex(entity.GetProperty(propertyName)) is { } index)
        {
            entity.RemoveIndex(index);
        }
    }
}
