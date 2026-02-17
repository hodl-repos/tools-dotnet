using System;
using System.Collections.Generic;

namespace tools_dotnet.Pagination.Models
{
    public sealed class DeserializedPaginationModel
    {
        public DeserializedPaginationModel(
            IReadOnlyList<PaginationFilterTerm> filters,
            IReadOnlyList<PaginationSortTerm> sorts,
            int page,
            int pageSize)
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

        public IReadOnlyList<PaginationFilterTerm> Filters { get; }

        public IReadOnlyList<PaginationSortTerm> Sorts { get; }

        public int Page { get; }

        public int PageSize { get; }
    }
}
