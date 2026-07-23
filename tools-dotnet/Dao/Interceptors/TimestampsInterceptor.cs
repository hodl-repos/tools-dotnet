using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Time;

namespace tools_dotnet.Dao.Interceptors
{
    /// <summary>Updates auditable entity timestamps during EF Core save operations.</summary>
    public class TimestampsInterceptor : SaveChangesInterceptor
    {
        private readonly IClockProvider _clockProvider;

        /// <summary>Initializes a new instance of <c>TimestampsInterceptor</c>.</summary>
        public TimestampsInterceptor(IClockProvider? clockProvider = null)
        {
            _clockProvider = clockProvider ?? SystemClockProvider.Instance;
        }

        /// <summary>Updates entity timestamps before a synchronous EF Core save.</summary>
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result
        )
        {
            AdjustTimestamps(eventData.Context?.ChangeTracker);

            return base.SavingChanges(eventData, result);
        }

        /// <summary>Updates entity timestamps before an asynchronous EF Core save.</summary>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default
        )
        {
            AdjustTimestamps(eventData.Context?.ChangeTracker);

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void AdjustTimestamps(ChangeTracker? changeTracker)
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

            var now = _clockProvider.UtcNow;

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
