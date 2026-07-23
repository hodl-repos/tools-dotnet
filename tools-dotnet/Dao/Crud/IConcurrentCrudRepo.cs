using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Crud
{
    /// <summary>Defines CRUD operations protected by optimistic concurrency tokens.</summary>
    public interface IConcurrentCrudRepo<TEntity, TIdType, TConcurrencyToken>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
    {
        /// <summary>Adds a new item asynchronously.</summary>
        Task<TIdType> AddAsync(TEntity item, CancellationToken cancellationToken = default);

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

        /// <summary>Finds a single item that matches a predicate asynchronously.</summary>
        Task<TEntity?> FindAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves an item by its identifier asynchronously.</summary>
        Task<TEntity> GetByIdAsync(TIdType id, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing item asynchronously.</summary>
        Task UpdateAsync(
            TEntity item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        /// <summary>Removes an item asynchronously.</summary>
        Task RemoveAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves the current concurrency token for an item asynchronously.</summary>
        Task<TConcurrencyToken> GetConcurrencyTokenAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        );
    }
}
