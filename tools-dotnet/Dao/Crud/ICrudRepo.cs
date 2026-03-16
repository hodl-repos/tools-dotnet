using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Crud
{
    public interface ICrudRepo<TEntity, TIdType> : ISortFilterAndPageRepo<TEntity>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
    {
        Task<TIdType> AddAsync(TEntity item, CancellationToken cancellationToken = default);

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

        Task<TEntity?> FindAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            CancellationToken cancellationToken = default
        );

        Task<TEntity> GetByIdAsync(TIdType id, CancellationToken cancellationToken = default);

        Task UpdateAsync(TEntity item, CancellationToken cancellationToken = default);

        Task RemoveAsync(TIdType id, CancellationToken cancellationToken = default);
    }
}
