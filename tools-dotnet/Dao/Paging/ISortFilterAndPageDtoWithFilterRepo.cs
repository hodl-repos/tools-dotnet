using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Paging
{
    /// <summary>Defines paged DTO projections that combine dynamic and typed filters.</summary>
    public interface ISortFilterAndPageDtoWithFilterRepo<TEntity, TDto> :
        ISortFilterAndPageWithFilterRepo<TEntity>
        where TEntity : class
    {
        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> additionalFilter,
            CancellationToken cancellationToken = default
        );
    }
}
