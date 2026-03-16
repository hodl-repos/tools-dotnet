using System.Linq;

namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Marker interface for custom sort method containers.
    /// </summary>
    /// <remarks>
    /// Public instance methods on implementations can be used as sort handlers when no mapped member matches a sort field.
    /// Supported method signatures:
    /// <c>IQueryable&lt;TEntity&gt; MethodName(IQueryable&lt;TEntity&gt; source)</c>,
    /// <c>IQueryable&lt;TEntity&gt; MethodName(IQueryable&lt;TEntity&gt; source, bool useThenBy)</c>,
    /// <c>IQueryable&lt;TEntity&gt; MethodName(IQueryable&lt;TEntity&gt; source, bool useThenBy, bool desc)</c>,
    /// and the same with an additional fourth <c>object[]?</c> argument for custom data.
    /// </remarks>
    public interface IPaginationCustomSortsMethods { }
}

