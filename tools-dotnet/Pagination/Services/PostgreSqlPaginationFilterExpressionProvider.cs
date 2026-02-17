namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Filter provider preset for PostgreSQL deployments.
    /// </summary>
    public sealed class PostgreSqlPaginationFilterExpressionProvider : DefaultPaginationFilterExpressionProvider
    {
        /// <summary>
        /// Creates a PostgreSQL preset provider.
        /// </summary>
        /// <param name="caseInsensitiveNormalization">
        /// Normalization strategy for case-insensitive operators.
        /// Use <see cref="PaginationCaseInsensitiveNormalization.None"/> for citext-based comparisons.
        /// </param>
        public PostgreSqlPaginationFilterExpressionProvider(
            PaginationCaseInsensitiveNormalization caseInsensitiveNormalization = PaginationCaseInsensitiveNormalization.ToLower)
            : base(caseInsensitiveNormalization)
        {
        }
    }
}
