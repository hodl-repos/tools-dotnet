using System;

namespace tools_dotnet.Dao.Entity
{
    /// <summary>
    /// Represents an entity that supports soft-delete auditing.
    /// </summary>
    public interface IAuditableEntity : IChangeTrackingEntity
    {
        /// <summary>
        /// Gets or sets the timestamp when the entity was soft-deleted.
        /// </summary>
        public DateTimeOffset? DeletedTimestamp { get; set; }
    }
}
