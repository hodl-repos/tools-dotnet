using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Crud
{
    public interface ISoftDeleteReadRepo<TEntity, TIdType>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
    {
        Task<IEnumerable<TEntity>> GetAllAsync(
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> filters,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filters,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        Task<TEntity?> FindAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        Task<TEntity> GetByIdAsync(
            TIdType id,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );
    }

    public interface ISoftDeleteCrudRepo<TEntity, TIdType>
        : ICrudRepo<TEntity, TIdType>,
            ISoftDeleteReadRepo<TEntity, TIdType>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
    {
        Task RestoreAsync(TIdType id, CancellationToken cancellationToken = default);

        Task HardRemoveAsync(TIdType id, CancellationToken cancellationToken = default);
    }
}
