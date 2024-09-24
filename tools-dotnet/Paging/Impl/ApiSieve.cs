using System.ComponentModel.DataAnnotations;

namespace tools_dotnet.Paging.Impl
{
    public class ApiSieve : IApiSieve
    {
        public string? Filters { get; set; } = string.Empty;

        public string? Sorts { get; set; } = string.Empty;

        public int? Page { get; set; }

        [Range(1, 500)]
        public int PageSize { get; set; } = 25;
    }
}