namespace tools_dotnet.Pagination.Models
{
    /// <summary>
    /// Raw pagination request model before parsing.
    /// </summary>
    public class PaginationModel
    {
        /// <summary>
        /// Gets or sets raw filter text.
        /// </summary>
        public string? Filters { get; set; }

        /// <summary>
        /// Gets or sets raw sort text.
        /// </summary>
        public string? Sorts { get; set; }

        /// <summary>
        /// Gets or sets one-based page number.
        /// </summary>
        public int? Page { get; set; }

        /// <summary>
        /// Gets or sets page size.
        /// </summary>
        public int? PageSize { get; set; }
    }
}
