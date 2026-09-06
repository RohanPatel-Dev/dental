using Dental.Modules.Auditing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Modules.Auditing.Data.Configurations;

/// <summary>EF mapping for <see cref="AuditTrail"/>.</summary>
public sealed class AuditTrailConfiguration : IEntityTypeConfiguration<AuditTrail>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("audit_trails");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityName).HasMaxLength(256).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(128).IsRequired();
        builder.Property(a => a.Operation).HasMaxLength(32).IsRequired();
        builder.Property(a => a.Module).HasMaxLength(256).IsRequired();
        builder.Property(a => a.CorrelationId).HasMaxLength(128);
        builder.Property(a => a.ChangedColumns).HasMaxLength(4000);
        builder.Property(a => a.TenantId).HasMaxLength(64).IsRequired();

        builder.Property(a => a.OldValues).HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnType("jsonb");

        builder.HasIndex(a => new { a.EntityName, a.EntityId })
            .HasDatabaseName("ix_audit_trails_entity");

        builder.HasIndex(a => a.OccurredOnUtc)
            .IsDescending()
            .HasDatabaseName("ix_audit_trails_occurred");

        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_audit_trails_user");
    }
}
