using System;

namespace tools_dotnet.Dto
{
    /// <summary>
    /// Represents a DTO that carries change-tracking timestamps for optimistic concurrency.
    /// </summary>
    public interface IChangeTrackingDto : IDto
    {
        /// <summary>
        /// Gets or sets the timestamp when the resource was created.
        /// </summary>
        public DateTimeOffset CreatedTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the resource was last updated.
        /// </summary>
        public DateTimeOffset? UpdatedTimestamp { get; set; }
    }
}
