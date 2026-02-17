using System.ComponentModel.DataAnnotations;

namespace tools_dotnet.Paging.Impl
{
    /// <summary>
    /// Default implementation of <see cref="IApiPagination"/> for query binding.
    /// </summary>
    public class ApiPagination : IApiPagination
    {
        /// <inheritdoc />
        public string? Filters { get; set; } = string.Empty;

        /// <inheritdoc />
        public string? Sorts { get; set; } = string.Empty;

        /// <inheritdoc />
        public int? Page { get; set; }

        /// <inheritdoc />
        [Range(1, 500)]
        public int PageSize { get; set; } = 25;
    }
}
