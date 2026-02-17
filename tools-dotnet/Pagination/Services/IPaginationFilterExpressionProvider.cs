using System.Linq.Expressions;

namespace tools_dotnet.Pagination.Services
{
    public interface IPaginationFilterExpressionProvider
    {
        bool TryBuildExpression(PaginationFilterExpressionContext context, out Expression? expression);
    }
}
