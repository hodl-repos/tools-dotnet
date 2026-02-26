using System;

namespace tools_dotnet.Pagination.Attributes
{
    /// <summary>
    /// Marks a member as available for pagination filter/sort mapping.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PaginationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether the member can be used in filters.
        /// </summary>
        public bool CanFilter { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the member can be used in sorting.
        /// </summary>
        public bool CanSort { get; set; } = true;

        /// <summary>
        /// Gets or sets whether nested members below this member can be used in filters.
        /// </summary>
        public bool CanFilterSubProperties { get; set; } = false;

        /// <summary>
        /// Gets or sets whether nested members below this member can be used in sorting.
        /// </summary>
        public bool CanSortSubProperties { get; set; } = false;

        /// <summary>
        /// Gets or sets an external name for the member used in filter/sort queries.
        /// </summary>
        public string? Name { get; set; }
    }
}
