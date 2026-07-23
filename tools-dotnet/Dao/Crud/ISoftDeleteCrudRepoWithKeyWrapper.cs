using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Crud
{
    /// <summary>Defines key-wrapper entity queries that can include soft-deleted rows.</summary>
    public interface ISoftDeleteReadRepoWithKeyWrapper<TEntity, TKeyWrapper>
        where TEntity : class, IAuditableEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        /// <summary>Retrieves all available items asynchronously.</summary>
        Task<IEnumerable<TEntity>> GetAllAsync(
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items asynchronously.</summary>
        Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> filters,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items asynchronously.</summary>
        Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items asynchronously.</summary>
        Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filters,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves an item by its identifier asynchronously.</summary>
        Task<TEntity> GetByIdAsync(
            TKeyWrapper keyWrapper,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );
    }

    /// <summary>Defines key-wrapper CRUD operations with restore and permanent-delete support.</summary>
    public interface ISoftDeleteCrudRepoWithKeyWrapper<TEntity, TKeyWrapper>
        : ICrudRepoWithKeyWrapper<TEntity, TKeyWrapper>,
            ISoftDeleteReadRepoWithKeyWrapper<TEntity, TKeyWrapper>
        where TEntity : class, IAuditableEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        /// <summary>Restores a soft-deleted item asynchronously.</summary>
        Task RestoreAsync(TKeyWrapper keyWrapper, CancellationToken cancellationToken = default);

        /// <summary>Permanently removes an item asynchronously.</summary>
        Task HardRemoveAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );
    }
}
