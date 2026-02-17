namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Default filter provider preset for SQL Server style case-insensitive matching.
    /// </summary>
    public sealed class SqlServerPaginationFilterExpressionProvider : DefaultPaginationFilterExpressionProvider
    {
        /// <summary>
        /// Creates a SQL Server preset provider using upper-case normalization.
        /// </summary>
        public SqlServerPaginationFilterExpressionProvider()
            : base(PaginationCaseInsensitiveNormalization.ToUpper)
        {
        }
    }
}
