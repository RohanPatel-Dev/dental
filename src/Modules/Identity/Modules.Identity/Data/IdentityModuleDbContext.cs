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

        // LAST: installs Finbuckle's anonymous tenant filter over everything marked above.
        builder.ConfigureMultiTenant();
    }
}
