using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class AuditableEntitiesInterceptor(ICurrentUserService currentUserService) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            UpdateAuditableEntities(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableEntities(DbContext context)
    {
        var utcNow = DateTime.UtcNow;
        var entries = context.ChangeTracker.Entries<Entity>().ToArray();

        var currentUserId = currentUserService.GetId();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    SetCurrentPropertyValue(
                        entry, nameof(Entity.Created), new DateTimeOffset(utcNow));
                    SetCurrentPropertyValue(
                        entry, nameof(Entity.CreatedBy), currentUserId ?? Guid.Empty);
                    break;
                case EntityState.Modified:
                    SetCurrentPropertyValue(
                        entry, nameof(Entity.LastModified), new DateTimeOffset(utcNow));
                    SetCurrentPropertyValue(
                        entry, nameof(Entity.ModifiedBy), currentUserId ?? Guid.Empty);
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                default:
                    continue;
            }
        }
    }

    private static void SetCurrentPropertyValue<T>(
        EntityEntry entry,
        string propertyName,
        T value) =>
        entry.Property(propertyName).CurrentValue = value;
}