using Dental.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Modules.Identity.Data.Configurations;

/// <summary>EF mapping for <see cref="DentalUser"/>.</summary>
public sealed class DentalUserConfiguration : IEntityTypeConfiguration<DentalUser>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DentalUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("users");

        builder.Property(u => u.FirstName).HasMaxLength(128).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(128).IsRequired();
        builder.Property(u => u.AvatarKey).HasMaxLength(512);
        builder.Property(u => u.TenantId).HasMaxLength(64).IsRequired();

        // Email is unique WITHIN a tenant, not globally: the same dentist may work at two practices.
        builder.HasIndex(u => new { u.TenantId, u.NormalizedEmail })
            .IsUnique()
            .HasDatabaseName("ux_users_tenant_email");

        builder.Ignore(u => u.FullName);
    }
}

/// <summary>EF mapping for <see cref="DentalRole"/>.</summary>
public sealed class DentalRoleConfiguration : IEntityTypeConfiguration<DentalRole>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DentalRole> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("roles");

        builder.Property(r => r.Description).HasMaxLength(512);
        builder.Property(r => r.TenantId).HasMaxLength(64).IsRequired();

        builder.HasIndex(r => new { r.TenantId, r.NormalizedName })
            .IsUnique()
            .HasDatabaseName("ux_roles_tenant_name");
    }
}

/// <summary>EF mapping for <see cref="RefreshToken"/>.</summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.App).HasMaxLength(32).IsRequired();
        builder.Property(t => t.TenantId).HasMaxLength(64).IsRequired();

        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("ux_refresh_tokens_hash");
        builder.HasIndex(t => new { t.UserId, t.SessionId }).HasDatabaseName("ix_refresh_tokens_session");
    }
}

/// <summary>Renames the remaining ASP.NET Identity tables to the project's snake_case convention.</summary>
public sealed class IdentityTableNameConfiguration :
    IEntityTypeConfiguration<IdentityUserRole<Guid>>,
    IEntityTypeConfiguration<IdentityUserClaim<Guid>>,
    IEntityTypeConfiguration<IdentityUserLogin<Guid>>,
    IEntityTypeConfiguration<IdentityUserToken<Guid>>,
    IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder) =>
        builder?.ToTable("user_roles");

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) =>
        builder?.ToTable("user_claims");

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) =>
        builder?.ToTable("user_logins");

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) =>
        builder?.ToTable("user_tokens");

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder) =>
        builder?.ToTable("role_claims");
}
