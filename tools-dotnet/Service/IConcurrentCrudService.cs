using System.Collections.Generic;
using System.Threading.Tasks;
using tools_dotnet.Dto;
using tools_dotnet.Paging;

namespace tools_dotnet.Service
{
    /// <summary>Defines validated DTO CRUD operations protected by optimistic concurrency tokens.</summary>
    public interface IConcurrentCrudService<TDto, TIdType, TConcurrencyToken>
        where TDto : class, IDtoWithId<TIdType>
        where TIdType : struct
    {
        /// <summary>Adds a new item asynchronously.</summary>
        Task<TIdType> AddAsync(TDto item, CancellationToken cancellationToken = default);

        /// <summary>Retrieves all available items asynchronously.</summary>
        Task<IEnumerable<TDto>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Retrieves all available items asynchronously.</summary>
        Task<IPagedList<TDto>> GetAllAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves an item by its identifier asynchronously.</summary>
        Task<TDto> GetByIdAsync(TIdType id, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing item asynchronously.</summary>
        Task UpdateAsync(
            TDto item,
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
