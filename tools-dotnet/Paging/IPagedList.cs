using System.Collections.Generic;
using tools_dotnet.Paging.Impl;

namespace tools_dotnet.Paging
{
    public interface IPagedList<out T>
    {
        public IReadOnlyList<T> Items { get; }

        public PagingMetadata Metadata { get; }
    }
}