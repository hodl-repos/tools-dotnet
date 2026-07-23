using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Crud
{
    /// <summary>Defines CRUD operations for entities addressed through a scoped key wrapper.</summary>
    public interface ICrudRepoWithKeyWrapper<TEntity, TKeyWrapper> : ISortFilterAndPageRepo<TEntity>
        where TEntity : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        /// <summary>Adds a new item asynchronously.</summary>
        Task<TKeyWrapper> AddAsync(
            TKeyWrapper keyWrapper,
            TEntity item,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items asynchronously.</summary>
        Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Retrieves all available items asynchronously.</summary>
        Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> filters,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items asynchronously.</summary>
        new Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items asynchronously.</summary>
        new Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filters,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves an item by its identifier asynchronously.</summary>
        Task<TEntity> GetByIdAsync(TKeyWrapper keyWrapper, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing item asynchronously.</summary>
        Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TEntity item,
            CancellationToken cancellationToken = default
        );

        /// <summary>Removes an item asynchronously.</summary>
        Task RemoveAsync(TKeyWrapper keyWrapper, CancellationToken cancellationToken = default);
    }
}
