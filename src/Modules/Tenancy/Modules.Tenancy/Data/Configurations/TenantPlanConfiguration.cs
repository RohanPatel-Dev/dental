using Dental.Modules.Tenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Modules.Tenancy.Data.Configurations;

/// <summary>EF mapping for <see cref="TenantPlan"/>.</summary>
public sealed class TenantPlanConfiguration : IEntityTypeConfiguration<TenantPlan>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TenantPlan> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tenant_plans");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(512).IsRequired();
        builder.Property(p => p.TenantId).HasMaxLength(64).IsRequired();

        builder.HasIndex(p => p.Name).IsUnique().HasDatabaseName("ux_tenant_plans_name");
    }
}
