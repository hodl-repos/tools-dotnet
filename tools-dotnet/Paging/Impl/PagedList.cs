using System.Collections.Generic;
using System.Linq;

namespace tools_dotnet.Paging.Impl
{
    public class PagedList<T> : IPagedList<T>
    {
        public PagedList()
        { }

        public PagedList(IEnumerable<T> items, int pageNumber, int? pageSize, int totalItemCount)
        {
            Items = items.ToList();

            Metadata = new PagingMetadata(pageNumber, pageSize, totalItemCount);
        }

        public PagedList(List<T> items, int pageNumber, int? pageSize, int totalItemCount)
        {
            Items = items;

            Metadata = new PagingMetadata(pageNumber, pageSize, totalItemCount);
        }

        public IReadOnlyList<T> Items { get; set; } = new List<T>();
        public PagingMetadata Metadata { get; set; } = new PagingMetadata(1, null, 0);
    }
}