using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Crud
{
    /// <summary>Defines key-wrapper CRUD operations protected by optimistic concurrency tokens.</summary>
    public interface IConcurrentCrudRepoWithKeyWrapper<TEntity, TKeyWrapper, TConcurrencyToken>
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
        Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items asynchronously.</summary>
        Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filters,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves an item by its identifier asynchronously.</summary>
        Task<TEntity> GetByIdAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        /// <summary>Updates an existing item asynchronously.</summary>
        Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TEntity item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        /// <summary>Removes an item asynchronously.</summary>
        Task RemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves the current concurrency token for an item asynchronously.</summary>
        Task<TConcurrencyToken> GetConcurrencyTokenAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );
    }
}
