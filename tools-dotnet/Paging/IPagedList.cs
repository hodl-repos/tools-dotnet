using System.Collections.Generic;
using tools_dotnet.Paging.Impl;

namespace tools_dotnet.Paging
{
    /// <summary>
    /// A paged response that contains items and page metadata.
    /// </summary>
    /// <typeparam name="T">Item type in the page.</typeparam>
    public interface IPagedList<out T>
    {
        /// <summary>
        /// Gets the items for the current page.
        /// </summary>
        public IReadOnlyList<T> Items { get; }

        /// <summary>
        /// Gets metadata for the current page.
        /// </summary>
        public PagingMetadata Metadata { get; }
    }
}
