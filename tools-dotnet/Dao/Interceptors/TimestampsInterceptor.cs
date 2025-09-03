using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;

namespace tools_dotnet.Dao.Interceptors
{
    public class TimestampsInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            AdjustTimestamps(eventData.Context?.ChangeTracker);

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
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
                .Where(e => e.Entity is IChangeTrackingEntity && (
                        e.State == EntityState.Added
                        || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                ((IChangeTrackingEntity)entityEntry.Entity).UpdatedTimestamp = DateTimeOffset.UtcNow;

                if (entityEntry.State == EntityState.Added)
                {
                    ((IChangeTrackingEntity)entityEntry.Entity).CreatedTimestamp = DateTimeOffset.UtcNow;
                }
            }
        }
    }
}