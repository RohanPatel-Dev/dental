using Dental.Modules.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dental.Modules.Billing.Data.Configurations;

/// <summary>EF mapping for <see cref="Invoice"/>.</summary>
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Number).HasMaxLength(32).IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(3).IsRequired();
        builder.Property(i => i.VoidReason).HasMaxLength(512);
        builder.Property(i => i.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(32);

        builder.HasMany(i => i.Lines)
            .WithOne()
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Payments)
            .WithOne()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.TenantId, i.Number })
            .IsUnique()
            .HasDatabaseName("ux_invoices_tenant_number");

        builder.HasIndex(i => new { i.PatientId, i.Status })
            .HasDatabaseName("ix_invoices_patient_status");

        builder.Ignore(i => i.DomainEvents);
        builder.Ignore(i => i.Total);
        builder.Ignore(i => i.AmountPaid);
        builder.Ignore(i => i.Balance);
    }
}

/// <summary>EF mapping for <see cref="InvoiceLine"/>.</summary>
public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("invoice_lines");
        builder.HasKey(l => l.Id);

        // Child reached only through the parent's collection.
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.ProcedureCode).HasMaxLength(32).IsRequired();
        builder.Property(l => l.Description).HasMaxLength(512).IsRequired();
        builder.Property(l => l.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(l => l.Amount).HasPrecision(18, 2);

        builder.HasIndex(l => l.AppointmentId).HasDatabaseName("ix_invoice_lines_appointment");
    }
}

/// <summary>EF mapping for <see cref="Payment"/>.</summary>
public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("payments");
        builder.HasKey(p => p.Id);

        // Child reached only through the parent's collection.
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Reference).HasMaxLength(128);
        builder.Property(p => p.TenantId).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(32);
        builder.Property(p => p.Amount).HasPrecision(18, 2);
    }
}
