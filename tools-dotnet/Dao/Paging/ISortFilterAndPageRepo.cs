using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Paging
{
    public interface ISortFilterAndPageRepo<TEntity> where TEntity : class
    {
        Task<IPagedList<TEntity>> GetAllAsync(IApiPagination apiPagination);

        Task<IPagedList<TEntity>> GetAllAsync(IApiPagination apiPagination, Expression<Func<TEntity, bool>> filter);
    }
}