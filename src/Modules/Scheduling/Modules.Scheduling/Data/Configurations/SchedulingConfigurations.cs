using Dental.Modules.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Modules.Scheduling.Data.Configurations;

/// <summary>EF mapping for <see cref="Appointment"/>.</summary>
public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("appointments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Notes).HasMaxLength(2000);
        builder.Property(a => a.CancellationReason).HasMaxLength(512);
        builder.Property(a => a.TenantId).HasMaxLength(64).IsRequired();

        builder.Property(a => a.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(32);

        // The index the conflict check and the day view both ride on.
        builder.HasIndex(a => new { a.ProviderId, a.StartsAt })
            .HasDatabaseName("ix_appointments_provider_start");

        builder.HasIndex(a => new { a.OperatoryId, a.StartsAt })
            .HasDatabaseName("ix_appointments_operatory_start");

        builder.HasIndex(a => a.PatientId).HasDatabaseName("ix_appointments_patient");

        builder.Ignore(a => a.DomainEvents);
        builder.Ignore(a => a.Slot);
        builder.Ignore(a => a.OccupiesSlot);
    }
}

/// <summary>EF mapping for <see cref="Provider"/>.</summary>
public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("providers");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(p => p.Speciality).HasMaxLength(128);
        builder.Property(p => p.TenantId).HasMaxLength(64).IsRequired();

        builder.HasIndex(p => p.UserId).HasDatabaseName("ix_providers_user");
    }
}

/// <summary>EF mapping for <see cref="Operatory"/>.</summary>
public sealed class OperatoryConfiguration : IEntityTypeConfiguration<Operatory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Operatory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("operatories");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).HasMaxLength(128).IsRequired();
        builder.Property(o => o.TenantId).HasMaxLength(64).IsRequired();

        builder.HasIndex(o => new { o.TenantId, o.Name })
            .IsUnique()
            .HasDatabaseName("ux_operatories_tenant_name");
    }
}
