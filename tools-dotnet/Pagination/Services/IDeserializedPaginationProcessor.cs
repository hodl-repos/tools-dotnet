using System.Linq;
using tools_dotnet.Pagination.Models;

namespace tools_dotnet.Pagination.Services
{
    internal interface IDeserializedPaginationProcessor
    {
        DeserializedPaginationModel Deserialize(PaginationModel model);

        IQueryable<TEntity> Apply<TEntity>(
            DeserializedPaginationModel model,
            IQueryable<TEntity> source,
            object[]? dataForCustomMethods = null,
            bool applyFiltering = true,
            bool applySorting = true,
            bool applyPagination = true
        );
    }
}
