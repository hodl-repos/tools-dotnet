using tools_dotnet.Pagination.Models;
using System.Linq;

namespace tools_dotnet.Pagination.Services
{
    public interface IPaginationProcessor
    {
        IQueryable<TEntity> Apply<TEntity>(
            PaginationModel model,
            IQueryable<TEntity> source,
            object[]? dataForCustomMethods = null,
            bool applyFiltering = true,
            bool applySorting = true,
            bool applyPagination = true);
    }
}
