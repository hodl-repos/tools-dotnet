using System;
using System.Collections.Generic;

namespace tools_dotnet.Pagination.Models
{
    /// <summary>
    /// Parsed and validated pagination model used by the processor.
    /// </summary>
    public sealed class DeserializedPaginationModel
    {
        /// <summary>
        /// Creates a parsed pagination model.
        /// </summary>
        /// <param name="filters">Parsed filter terms.</param>
        /// <param name="sorts">Parsed sort terms.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Page size.</param>
        public DeserializedPaginationModel(
            IReadOnlyList<PaginationFilterTerm> filters,
            IReadOnlyList<PaginationSortTerm> sorts,
            int page,
            int pageSize
        )
        {
            if (page < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be >= 1.");
            }

            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be >= 1.");
            }

            Filters = filters ?? throw new ArgumentNullException(nameof(filters));
            Sorts = sorts ?? throw new ArgumentNullException(nameof(sorts));
            Page = page;
            PageSize = pageSize;
        }

        /// <summary>
        /// Gets parsed filter terms.
        /// </summary>
        public IReadOnlyList<PaginationFilterTerm> Filters { get; }

        /// <summary>
        /// Gets parsed sort terms.
        /// </summary>
        public IReadOnlyList<PaginationSortTerm> Sorts { get; }

        /// <summary>
        /// Gets one-based page number.
        /// </summary>
        public int Page { get; }

        /// <summary>
        /// Gets page size.
        /// </summary>
        public int PageSize { get; }
    }
}
