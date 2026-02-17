using System;

namespace tools_dotnet.Dao.Entity
{
    /// <summary>
    /// Represents an entity that tracks creation and update timestamps.
    /// </summary>
    public interface IChangeTrackingEntity : IEntity
    {
        /// <summary>
        /// Gets or sets the timestamp when the entity was first created.
        /// </summary>
        public DateTimeOffset CreatedTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the entity was last updated.
        /// </summary>
        public DateTimeOffset? UpdatedTimestamp { get; set; }
    }
}
