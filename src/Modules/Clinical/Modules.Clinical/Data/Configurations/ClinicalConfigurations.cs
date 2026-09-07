using Dental.Modules.Clinical.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Modules.Clinical.Data.Configurations;

/// <summary>EF mapping for <see cref="Procedure"/>.</summary>
public sealed class ProcedureConfiguration : IEntityTypeConfiguration<Procedure>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Procedure> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("procedures");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(32).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(512).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Category).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.DefaultFee).HasPrecision(18, 2);

        builder.HasIndex(p => new { p.TenantId, p.Code })
            .IsUnique()
            .HasDatabaseName("ux_procedures_tenant_code");
    }
}

/// <summary>EF mapping for <see cref="TreatmentPlan"/>.</summary>
public sealed class TreatmentPlanConfiguration : IEntityTypeConfiguration<TreatmentPlan>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TreatmentPlan> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("treatment_plans");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Notes).HasMaxLength(4000);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);

        builder.HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey(i => i.TreatmentPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.PatientId).HasDatabaseName("ix_treatment_plans_patient");

        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.TotalFee);
    }
}

/// <summary>EF mapping for <see cref="TreatmentPlanItem"/>.</summary>
public sealed class TreatmentPlanItemConfiguration : IEntityTypeConfiguration<TreatmentPlanItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TreatmentPlanItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("treatment_plan_items");
        builder.HasKey(i => i.Id);

        // Child reached only through the parent's collection - see PatientDocument for why.
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.ProcedureCode).HasMaxLength(32).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(512).IsRequired();
        builder.Property(i => i.Surfaces).HasMaxLength(16);
        builder.Property(i => i.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(i => i.Fee).HasPrecision(18, 2);

        builder.HasIndex(i => i.DeliveredAtAppointmentId)
            .HasDatabaseName("ix_treatment_plan_items_appointment");
    }
}

/// <summary>EF mapping for <see cref="ChartEntry"/>.</summary>
public sealed class ChartEntryConfiguration : IEntityTypeConfiguration<ChartEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChartEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("chart_entries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Surfaces).HasMaxLength(16);
        builder.Property(e => e.Notes).HasMaxLength(4000);
        builder.Property(e => e.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.Condition).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(e => new { e.PatientId, e.ToothNumber, e.RecordedAt })
            .HasDatabaseName("ix_chart_entries_patient_tooth");
    }
}
