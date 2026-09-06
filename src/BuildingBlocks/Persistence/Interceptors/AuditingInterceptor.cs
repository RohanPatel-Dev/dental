using Dental.Framework.Core.Contracts;
using Dental.Framework.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dental.Framework.Persistence.Interceptors;

/// <summary>
/// Stamps <see cref="BaseEntity.CreatedAt"/>, <see cref="BaseEntity.UpdatedAt"/> and the
/// <see cref="IAuditableEntity"/> user columns on every save.
/// </summary>
/// <param name="currentUser">Caller of the current request, if any.</param>
/// <param name="timeProvider">Clock. Injected so tests can move time.</param>
public sealed class AuditingInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        Guid? userId = currentUser.UserId;

        foreach (EntityEntry<BaseEntity> entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    if (entry.Entity is IAuditableEntity added)
                    {
                        added.CreatedBy = userId;
                    }

                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    if (entry.Entity is IAuditableEntity modified)
                    {
                        modified.UpdatedBy = userId;
                    }

                    break;

                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                default:
                    break;
            }
        }
    }
}
