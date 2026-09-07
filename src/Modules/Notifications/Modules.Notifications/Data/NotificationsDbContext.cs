using Dental.Framework.Eventing.Extensions;
using Dental.Framework.Persistence.Contexts;
using Dental.Modules.Notifications.Domain;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Notifications.Data;

/// <summary>The outbound notification log.</summary>
/// <param name="multiTenantContextAccessor">Finbuckle accessor.</param>
/// <param name="options">EF options.</param>
public sealed class NotificationsDbContext(
    IMultiTenantContextAccessor multiTenantContextAccessor,
    DbContextOptions<NotificationsDbContext> options)
    : BaseDbContext(multiTenantContextAccessor, options)
{
    /// <summary>Schema name for this module.</summary>
    public const string SchemaName = "notifications";

    /// <summary>Queued and sent messages.</summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
        modelBuilder.ApplyEventingModel();

        // LAST, always.
        base.OnModelCreating(modelBuilder);
    }
}
