namespace tools_dotnet.Pagination.Services
{
    public sealed class SqlServerPaginationFilterExpressionProvider : DefaultPaginationFilterExpressionProvider
    {
        public SqlServerPaginationFilterExpressionProvider()
            : base(PaginationCaseInsensitiveNormalization.ToUpper)
        {
        }
    }
}
