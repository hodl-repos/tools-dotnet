namespace tools_dotnet.Pagination.Models
{
    public class PaginationModel
    {
        public string? Filters { get; set; }

        public string? Sorts { get; set; }

        public int? Page { get; set; }

        public int? PageSize { get; set; }
    }
}
