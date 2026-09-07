using Dental.Modules.Patients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Modules.Patients.Data.Configurations;

/// <summary>EF mapping for <see cref="Patient"/>.</summary>
public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("patients");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ChartNumber).HasMaxLength(32).IsRequired();
        builder.Property(p => p.FirstName).HasMaxLength(128).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(128).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Property(p => p.PhoneNumber).HasMaxLength(32);
        builder.Property(p => p.TenantId).HasMaxLength(64).IsRequired();

        builder.Property(p => p.Sex).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(16);

        builder.Property(p => p.Allergies).HasColumnType("jsonb");

        builder.HasIndex(p => new { p.TenantId, p.ChartNumber })
            .IsUnique()
            .HasDatabaseName("ux_patients_tenant_chart");

        builder.HasIndex(p => new { p.LastName, p.FirstName })
            .HasDatabaseName("ix_patients_name");

        builder.HasMany(p => p.Documents)
            .WithOne()
            .HasForeignKey(d => d.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Documents).AutoInclude(false);

        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.FullName);
    }
}
