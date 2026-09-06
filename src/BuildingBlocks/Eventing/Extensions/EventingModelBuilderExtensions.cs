using Dental.Framework.Eventing.Inbox;
using Dental.Framework.Eventing.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Dental.Framework.Eventing.Extensions;

/// <summary>Maps the outbox and inbox tables into a module's schema.</summary>
public static class EventingModelBuilderExtensions
{
    /// <summary>
    /// Adds the <c>outbox_messages</c> and <c>inbox_messages</c> tables. Call from a module
    /// <c>DbContext.OnModelCreating</c> BEFORE the final <c>base.OnModelCreating(modelBuilder)</c>.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder, for chaining.</returns>
    public static ModelBuilder ApplyEventingModel(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.EventType).HasMaxLength(512).IsRequired();
            entity.Property(m => m.Payload).IsRequired();
            entity.Property(m => m.CorrelationId).HasMaxLength(128);
            entity.Property(m => m.TenantId).HasMaxLength(64).IsRequired();
            entity.Property(m => m.Error).HasMaxLength(4000);
            entity.HasIndex(m => new { m.ProcessedOnUtc, m.IsDeadLettered, m.OccurredOnUtc })
                .HasDatabaseName("ix_outbox_pending");
        });

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("inbox_messages");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.HandlerName).HasMaxLength(512).IsRequired();
            entity.Property(m => m.EventType).HasMaxLength(512).IsRequired();
            entity.Property(m => m.TenantId).HasMaxLength(64).IsRequired();

            // The composite uniqueness IS the idempotency guarantee - a concurrent second delivery
            // loses the insert race and skips the handler.
            entity.HasIndex(m => new { m.EventId, m.HandlerName })
                .IsUnique()
                .HasDatabaseName("ux_inbox_event_handler");
        });

        return modelBuilder;
    }
}
