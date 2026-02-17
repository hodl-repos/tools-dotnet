namespace tools_dotnet.Pagination.Services
{
    public sealed class PostgreSqlPaginationFilterExpressionProvider : DefaultPaginationFilterExpressionProvider
    {
        public PostgreSqlPaginationFilterExpressionProvider(
            PaginationCaseInsensitiveNormalization caseInsensitiveNormalization = PaginationCaseInsensitiveNormalization.ToLower)
            : base(caseInsensitiveNormalization)
        {
        }
    }
}
