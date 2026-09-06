using Dental.Modules.Tenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Modules.Tenancy.Data.Configurations;

/// <summary>EF mapping for <see cref="Tenant"/>.</summary>
public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tenants");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Identifier).HasMaxLength(64).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(256).IsRequired();
        builder.Property(t => t.AdminEmail).HasMaxLength(256).IsRequired();
        builder.Property(t => t.PlanName).HasMaxLength(64).IsRequired();
        builder.Property(t => t.TimeZone).HasMaxLength(64).IsRequired();
        builder.Property(t => t.TenantId).HasMaxLength(64).IsRequired();

        builder.HasIndex(t => t.Identifier).IsUnique().HasDatabaseName("ux_tenants_identifier");
        builder.HasIndex(t => t.IsActive).HasDatabaseName("ix_tenants_active");

        builder.Ignore(t => t.DomainEvents);
    }
}
