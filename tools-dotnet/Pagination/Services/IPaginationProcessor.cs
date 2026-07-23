using System;
using System.Linq;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Models;

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
        /// <exception cref="ArgumentNullException"><paramref name="model"/> or <paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidPaginationFilterException">A filter is invalid, references an unavailable field, or contains a value that cannot be parsed.</exception>
        /// <exception cref="InvalidPaginationSortException">A sort references an unavailable field.</exception>
        IQueryable<TEntity> Apply<TEntity>(
            PaginationModel model,
            IQueryable<TEntity> source,
            object[]? dataForCustomMethods = null,
            bool applyFiltering = true,
            bool applySorting = true,
            bool applyPagination = true
        );
    }
}
