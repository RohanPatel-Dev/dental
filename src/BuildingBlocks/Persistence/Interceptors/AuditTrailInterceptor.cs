using System.Text.Json;
using Dental.Framework.Core.Contracts;
using Dental.Framework.Core.Domain;
using Dental.Framework.Shared.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dental.Framework.Persistence.Interceptors;

/// <summary>
/// Captures inserts, updates and soft deletes of <see cref="IAuditableEntity"/> rows and hands them
/// to the registered <see cref="IAuditSink"/>.
/// </summary>
/// <remarks>
/// Values are captured BEFORE the save (the change tracker is authoritative only then) and written
/// AFTER it succeeds, so a rolled back transaction leaves no audit trail. Sensitive property names
/// listed in <see cref="AuditRedaction.SensitiveProperties"/> never reach the sink.
/// </remarks>
/// <param name="sink">Where records are written. Optional - no sink means no auditing.</param>
/// <param name="currentUser">Caller of the current request, if any.</param>
/// <param name="requestContext">Supplies the correlation identifier.</param>
/// <param name="timeProvider">Clock.</param>
public sealed class AuditTrailInterceptor(
    IAuditSink? sink,
    ICurrentUser currentUser,
    IRequestContext requestContext,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    private readonly List<AuditRecord> _pending = [];

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (sink is not null)
        {
            Capture(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        int saved = await base.SavedChangesAsync(eventData, result, cancellationToken)
            .ConfigureAwait(false);

        if (sink is null || _pending.Count == 0)
        {
            return saved;
        }

        AuditRecord[] records = [.. _pending];
        _pending.Clear();

        await sink.WriteAsync(records, cancellationToken).ConfigureAwait(false);
        return saved;
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _pending.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        foreach (EntityEntry<IAuditableEntity> entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            string? operation = entry.State switch
            {
                EntityState.Added => AuditOperations.Created,
                EntityState.Modified => IsSoftDelete(entry)
                    ? AuditOperations.Deleted
                    : AuditOperations.Updated,
                EntityState.Deleted => AuditOperations.Deleted,
                _ => null,
            };

            if (operation is null)
            {
                continue;
            }

            _pending.Add(BuildRecord(entry, operation, now));
        }
    }

    private AuditRecord BuildRecord(
        EntityEntry<IAuditableEntity> entry,
        string operation,
        DateTimeOffset now)
    {
        Dictionary<string, object?> oldValues = new(StringComparer.Ordinal);
        Dictionary<string, object?> newValues = new(StringComparer.Ordinal);
        List<string> changed = [];

        foreach (PropertyEntry property in entry.Properties)
        {
            string name = property.Metadata.Name;

            if (AuditRedaction.SensitiveProperties.Contains(name))
            {
                if (property.IsModified || entry.State == EntityState.Added)
                {
                    changed.Add(name);
                    newValues[name] = AuditRedaction.Placeholder;
                    oldValues[name] = AuditRedaction.Placeholder;
                }

                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    newValues[name] = property.CurrentValue;
                    break;

                case EntityState.Deleted:
                    oldValues[name] = property.OriginalValue;
                    break;

                case EntityState.Modified when property.IsModified:
                    changed.Add(name);
                    oldValues[name] = property.OriginalValue;
                    newValues[name] = property.CurrentValue;
                    break;

                default:
                    break;
            }
        }

        string entityId = entry.Entity is BaseEntity baseEntity
            ? baseEntity.Id.ToString()
            : string.Join(
                ',',
                entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).Select(p => p.CurrentValue));

        string? tenantId = entry.Entity is IHasTenant tenanted ? tenanted.TenantId : null;

        return new AuditRecord(
            entry.Entity.GetType().Name,
            entityId,
            operation,
            entry.Entity.GetType().Assembly.GetName().Name ?? "unknown",
            tenantId,
            currentUser.UserId,
            now,
            requestContext.CorrelationId,
            changed,
            oldValues.Count == 0 ? null : JsonSerializer.Serialize(oldValues, JsonSerializerOptions.Web),
            newValues.Count == 0 ? null : JsonSerializer.Serialize(newValues, JsonSerializerOptions.Web));
    }

    private static bool IsSoftDelete(EntityEntry entry) =>
        entry.Entity is ISoftDeletable
        && entry.Properties.Any(p =>
            string.Equals(p.Metadata.Name, nameof(ISoftDeletable.DeletedAt), StringComparison.Ordinal)
            && p.IsModified
            && p.CurrentValue is not null);
}
