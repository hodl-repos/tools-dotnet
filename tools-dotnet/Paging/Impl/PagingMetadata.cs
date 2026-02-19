using System;
using System.Text.Json.Serialization;

namespace tools_dotnet.Paging.Impl
{
    /// <summary>
    /// Metadata describing a paged result set.
    /// </summary>
    public class PagingMetadata
    {
        /// <summary>
        /// Creates metadata from explicit values. Mainly used by deserializers.
        /// </summary>
        /// <param name="pageCount">Total number of pages.</param>
        /// <param name="totalItemCount">Total number of matching items.</param>
        /// <param name="pageNumber">Current one-based page number.</param>
        /// <param name="pageSize">Page size used for the current page.</param>
        [JsonConstructor]
        public PagingMetadata(int pageCount, int totalItemCount, int pageNumber, int? pageSize)
        {
            PageCount = pageCount;
            TotalItemCount = totalItemCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        /// <summary>
        /// Creates metadata and calculates <see cref="PageCount"/> from item count and page size.
        /// </summary>
        /// <param name="pageNumber">Current one-based page number.</param>
        /// <param name="pageSize">Requested page size.</param>
        /// <param name="totalItemCount">Total number of matching items.</param>
        public PagingMetadata(int pageNumber, int? pageSize, int totalItemCount)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalItemCount = totalItemCount;

            if (PageSize.HasValue && PageSize != 0)
            {
                PageCount = (int)Math.Ceiling(TotalItemCount / (double)PageSize);
            }
            else
            {
                PageCount = 1;
                PageSize = TotalItemCount;
            }
        }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int PageCount { get; }

        /// <summary>
        /// Gets the total number of matching items.
        /// </summary>
        public int TotalItemCount { get; }

        /// <summary>
        /// Gets the current one-based page number.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Gets the page size used for the result.
        /// </summary>
        public int? PageSize { get; }

        /// <summary>
        /// Gets a value indicating whether there is another page after this page.
        /// </summary>
        public bool HasNextPage
        {
            get
            {
                return PageNumber >= 1 && PageNumber < PageCount;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is the first page.
        /// </summary>
        public bool IsFirstPage
        {
            get
            {
                return PageNumber == 1;
            }
        }
    }
}
