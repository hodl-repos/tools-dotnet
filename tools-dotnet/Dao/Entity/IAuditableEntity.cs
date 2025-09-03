using System;

namespace tools_dotnet.Dao.Entity
{
    public interface IAuditableEntity : IChangeTrackingEntity
    {
        public DateTimeOffset? DeletedTimestamp { get; set; }
    }
}