namespace tools_dotnet.Paging
{
    /// <summary>
    /// Query contract used by API endpoints for filtering, sorting, and paging.
    /// </summary>
    public interface IApiPagination
    {
        /// <summary>
        /// Gets or sets raw filter text in pagination syntax.
        /// </summary>
        public string? Filters { get; set; }

        /// <summary>
        /// Gets or sets raw sort text in pagination syntax.
        /// </summary>
        public string? Sorts { get; set; }

        /// <summary>
        /// Gets or sets the one-based page number.
        /// </summary>
        public int? Page { get; set; }

        /// <summary>
        /// Gets or sets the requested page size.
        /// </summary>
        public int PageSize { get; set; }
    }
}
