using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Paging
{
    public interface ISortFilterAndPageDtoRepo<TEntity, TDto> :
        ISortFilterAndPageRepo<TEntity>
        where TEntity : class
    {
        Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        );

        Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        );
    }
}
