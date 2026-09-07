using Dental.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Modules.Notifications.Data.Configurations;

/// <summary>EF mapping for <see cref="Notification"/>.</summary>
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Subject).HasMaxLength(256).IsRequired();
        builder.Property(n => n.Recipient).HasMaxLength(256).IsRequired();
        builder.Property(n => n.Error).HasMaxLength(2000);
        builder.Property(n => n.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(n => n.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(32);

        // The index the dispatch sweep rides on.
        builder.HasIndex(n => new { n.Status, n.ScheduledFor })
            .HasDatabaseName("ix_notifications_due");

        builder.HasIndex(n => new { n.PatientId, n.SubjectId })
            .HasDatabaseName("ix_notifications_patient_subject");
    }
}
