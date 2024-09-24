using System;

namespace tools_dotnet.Dao.Entity
{
    public interface IAuditableEntity : IEntity
    {
        public DateTimeOffset CreatedTimestamp { get; set; }

        public DateTimeOffset? UpdatedTimestamp { get; set; }

        public DateTimeOffset? DeletedTimestamp { get; set; }
    }
}