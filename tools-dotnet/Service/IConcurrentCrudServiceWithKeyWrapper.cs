using System.Collections.Generic;
using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Paging;

namespace tools_dotnet.Service
{
    /// <summary>Defines concurrency-aware DTO CRUD operations addressed through a key wrapper.</summary>
    public interface IConcurrentCrudServiceWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TConcurrencyToken
    >
        where TDto : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        /// <summary>Adds a new item asynchronously.</summary>
        Task<TKeyWrapper> AddAsync(
            TKeyWrapper keyWrapper,
            TDto item,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items asynchronously.</summary>
        Task<IEnumerable<TDto>> GetAllAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items asynchronously.</summary>
        Task<IPagedList<TDto>> GetAllAsync(
            IApiPagination apiPagination,
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves an item by its identifier asynchronously.</summary>
        Task<TDto> GetByIdAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        /// <summary>Updates an existing item asynchronously.</summary>
        Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TDto item,
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
