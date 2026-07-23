using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Paging
{
    /// <summary>Defines paged entity queries that combine dynamic and typed filters.</summary>
    public interface ISortFilterAndPageWithFilterRepo<TEntity> where TEntity : class
    {
        /// <summary>Retrieves all available items asynchronously.</summary>
        Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> additionalFilter,
            CancellationToken cancellationToken = default
        );
    }
}
