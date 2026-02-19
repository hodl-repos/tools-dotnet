using tools_dotnet.Pagination.Models;
using System.Linq;

namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Applies filtering, sorting, and pagination to queryables.
    /// </summary>
    public interface IPaginationProcessor
    {
        /// <summary>
        /// Applies pagination operations to a source query.
        /// </summary>
        /// <typeparam name="TEntity">Entity type in the source query.</typeparam>
        /// <param name="model">Raw model containing filters, sorts, and page values.</param>
        /// <param name="source">Source query.</param>
        /// <param name="dataForCustomMethods">Optional extra data for custom providers.</param>
        /// <param name="applyFiltering">Whether to apply filter terms.</param>
        /// <param name="applySorting">Whether to apply sort terms.</param>
        /// <param name="applyPagination">Whether to apply skip/take pagination.</param>
        /// <returns>Updated query with requested operations applied.</returns>
        IQueryable<TEntity> Apply<TEntity>(
            PaginationModel model,
            IQueryable<TEntity> source,
            object[]? dataForCustomMethods = null,
            bool applyFiltering = true,
            bool applySorting = true,
            bool applyPagination = true);
    }
}