using System.Linq.Expressions;

namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Builds expression trees for parsed filter terms.
    /// </summary>
    public interface IPaginationFilterExpressionProvider
    {
        /// <summary>
        /// Tries to build a filter expression for the given context.
        /// </summary>
        /// <param name="context">Filter context with member and typed values.</param>
        /// <param name="expression">Built expression when successful.</param>
        /// <returns><see langword="true"/> when an expression was built; otherwise <see langword="false"/>.</returns>
        bool TryBuildExpression(PaginationFilterExpressionContext context, out Expression? expression);
    }
}
