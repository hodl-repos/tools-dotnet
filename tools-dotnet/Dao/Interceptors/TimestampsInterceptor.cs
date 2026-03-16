using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using tools_dotnet.Dao.Entity;

namespace tools_dotnet.Dao.Interceptors
{
    public class TimestampsInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result
        )
        {
            AdjustTimestamps(eventData.Context?.ChangeTracker);

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default
        )
        {
            AdjustTimestamps(eventData.Context?.ChangeTracker);

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static void AdjustTimestamps(ChangeTracker? changeTracker)
        {
            if (changeTracker == null)
            {
                return;
            }

            var entries = changeTracker
                .Entries()
                .Where(e =>
                    e.Entity is IChangeTrackingEntity
                    && (e.State == EntityState.Added || e.State == EntityState.Modified)
                );

            var now = DateTimeOffset.UtcNow;

            foreach (var entityEntry in entries)
            {
                var entity = (IChangeTrackingEntity)entityEntry.Entity;

                if (entityEntry.State == EntityState.Added)
                {
                    entity.CreatedTimestamp = now;
                    entity.UpdatedTimestamp = now;
                    continue;
                }

                PreserveCreatedTimestamp(entityEntry, entity);
                entity.UpdatedTimestamp = now;
            }
        }

        private static void PreserveCreatedTimestamp(
            EntityEntry entityEntry,
            IChangeTrackingEntity entity
        )
        {
            if (entityEntry.Metadata.FindProperty(nameof(IChangeTrackingEntity.CreatedTimestamp)) == null)
            {
                return;
            }

            var createdTimestampProperty = entityEntry.Property(
                nameof(IChangeTrackingEntity.CreatedTimestamp)
            );
            var originalCreatedTimestamp = (DateTimeOffset)createdTimestampProperty.OriginalValue!;

            createdTimestampProperty.CurrentValue = originalCreatedTimestamp;
            createdTimestampProperty.IsModified = false;
            entity.CreatedTimestamp = originalCreatedTimestamp;
        }
    }
}
