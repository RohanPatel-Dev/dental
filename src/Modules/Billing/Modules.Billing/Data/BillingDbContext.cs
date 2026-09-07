using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Contexts;
using Dental.Modules.Billing.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Billing.Data;

/// <summary>The ledger: invoices, their lines and the payments received.</summary>
/// <param name="multiTenantContextAccessor">Finbuckle accessor.</param>
/// <param name="options">EF options.</param>
public sealed class BillingDbContext(
    IMultiTenantContextAccessor multiTenantContextAccessor,
    DbContextOptions<BillingDbContext> options)
    : BaseDbContext(multiTenantContextAccessor, options)
{
    /// <summary>Schema name for this module.</summary>
    public const string SchemaName = "billing";

    /// <summary>Invoices.</summary>
    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <summary>Invoice lines.</summary>
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    /// <summary>Payments.</summary>
    public DbSet<Payment> Payments => Set<Payment>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
        modelBuilder.ApplyEventingModel();

        // LAST, always.
        base.OnModelCreating(modelBuilder);
    }
}
