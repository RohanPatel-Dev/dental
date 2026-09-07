using Dental.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Modules.Patients.Data.Configurations;

/// <summary>EF mapping for <see cref="PatientDocument"/>.</summary>
public sealed class PatientDocumentConfiguration : IEntityTypeConfiguration<PatientDocument>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PatientDocument> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("patient_documents");
        builder.HasKey(d => d.Id);

        // REQUIRED for a child reached only through the parent's navigation collection: without it
        // EF treats a newly added document as Modified rather than Added and the insert misbehaves.
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.FileName).HasMaxLength(256).IsRequired();
        builder.Property(d => d.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(128);
        builder.Property(d => d.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(d => d.FileType).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(d => d.PatientId).HasDatabaseName("ix_patient_documents_patient");
    }
}
