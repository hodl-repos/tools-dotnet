namespace tools_dotnet.Paging
{
    public interface IApiSieve
    {
        public string? Filters { get; set; }

        public string? Sorts { get; set; }

        public int? Page { get; set; }

        public int PageSize { get; set; }
    }
}