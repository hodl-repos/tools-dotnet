using System.Collections.Generic;
using System.Linq;

namespace tools_dotnet.Paging.Impl
{
    /// <summary>
    /// Default <see cref="IPagedList{T}"/> implementation.
    /// </summary>
    /// <typeparam name="T">Item type in the page.</typeparam>
    public class PagedList<T> : IPagedList<T>
    {
        /// <summary>
        /// Creates an empty paged list.
        /// </summary>
        public PagedList()
        { }

        /// <summary>
        /// Creates a paged list from an item sequence and paging values.
        /// </summary>
        /// <param name="items">Items for the current page.</param>
        /// <param name="pageNumber">Current one-based page number.</param>
        /// <param name="pageSize">Page size used for the result.</param>
        /// <param name="totalItemCount">Total number of matching items.</param>
        public PagedList(IEnumerable<T> items, int pageNumber, int? pageSize, int totalItemCount)
        {
            Items = items.ToList();

            Metadata = new PagingMetadata(pageNumber, pageSize, totalItemCount);
        }

        /// <summary>
        /// Creates a paged list from a materialized list and paging values.
        /// </summary>
        /// <param name="items">Items for the current page.</param>
        /// <param name="pageNumber">Current one-based page number.</param>
        /// <param name="pageSize">Page size used for the result.</param>
        /// <param name="totalItemCount">Total number of matching items.</param>
        public PagedList(List<T> items, int pageNumber, int? pageSize, int totalItemCount)
        {
            Items = items;

            Metadata = new PagingMetadata(pageNumber, pageSize, totalItemCount);
        }

        /// <inheritdoc />
        public IReadOnlyList<T> Items { get; set; } = new List<T>();

        /// <inheritdoc />
        public PagingMetadata Metadata { get; set; } = new PagingMetadata(1, null, 0);
    }
}
