using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Crud
{
    public interface ICrudRepoWithKeyWrapper<TEntity, TKeyWrapper> : ISortFilterAndPageRepo<TEntity>
        where TEntity : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        Task<TKeyWrapper> AddAsync(
            TKeyWrapper keyWrapper,
            TEntity item,
            CancellationToken cancellationToken = default
        );

        Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> filters,
            CancellationToken cancellationToken = default
        );

        Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        );

        Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filters,
            CancellationToken cancellationToken = default
        );

        Task<TEntity> GetByIdAsync(TKeyWrapper keyWrapper, CancellationToken cancellationToken = default);

        Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TEntity item,
            CancellationToken cancellationToken = default
        );

        Task RemoveAsync(TKeyWrapper keyWrapper, CancellationToken cancellationToken = default);
    }
}
