using System.Linq;

namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Marker interface for custom filter method containers.
    /// </summary>
    /// <remarks>
    /// Public instance methods on implementations can be used as filter handlers when no mapped member matches a filter field.
    /// Supported method signatures:
    /// <c>IQueryable&lt;TEntity&gt; MethodName(IQueryable&lt;TEntity&gt; source)</c>,
    /// <c>IQueryable&lt;TEntity&gt; MethodName(IQueryable&lt;TEntity&gt; source, string op)</c>,
    /// <c>IQueryable&lt;TEntity&gt; MethodName(IQueryable&lt;TEntity&gt; source, string op, string[] values)</c>,
    /// and the same with an additional fourth <c>object[]?</c> argument for custom data.
    /// </remarks>
    public interface IPaginationCustomFilterMethods
    {
    }
}
