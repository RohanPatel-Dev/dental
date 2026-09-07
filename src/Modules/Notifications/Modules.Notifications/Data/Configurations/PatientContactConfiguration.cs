using Dental.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Modules.Notifications.Data.Configurations;

/// <summary>EF mapping for <see cref="PatientContact"/>.</summary>
public sealed class PatientContactConfiguration : IEntityTypeConfiguration<PatientContact>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PatientContact> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("patient_contacts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.PhoneNumber).HasMaxLength(32);
        builder.Property(c => c.TenantId).HasMaxLength(64).IsRequired();

        // One projection row per patient per tenant; the upsert relies on this being unique.
        builder.HasIndex(c => new { c.TenantId, c.PatientId })
            .IsUnique()
            .HasDatabaseName("ux_patient_contacts_tenant_patient");

        builder.Ignore(c => c.IsContactable);
    }
}
