using System;
using System.Text.Json.Serialization;

namespace tools_dotnet.Paging.Impl
{
    public class PagingMetadata
    {
        [JsonConstructor]
        public PagingMetadata(int pageCount, int totalItemCount, int pageNumber, int? pageSize)
        {
            PageCount = pageCount;
            TotalItemCount = totalItemCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

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

        public int PageCount { get; }
        public int TotalItemCount { get; }
        public int PageNumber { get; }
        public int? PageSize { get; }

        public bool HasNextPage
        {
            get
            {
                return PageNumber >= 1 && PageNumber < PageCount;
            }
        }

        public bool IsFirstPage
        {
            get
            {
                return PageNumber == 1;
            }
        }
    }
}