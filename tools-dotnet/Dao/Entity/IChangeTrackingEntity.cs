using System;

namespace tools_dotnet.Dao.Entity
{
    public interface IChangeTrackingEntity : IEntity
    {
        public DateTimeOffset CreatedTimestamp { get; set; }

        public DateTimeOffset? UpdatedTimestamp { get; set; }
    }
}