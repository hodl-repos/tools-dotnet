using System.Collections.Generic;
using System.Threading.Tasks;
using tools_dotnet.Dto;

namespace tools_dotnet.Service
{
    /// <summary>Defines DTO queries that expose active and soft-deleted items.</summary>
    public interface ISoftDeleteReadService<TDto, TIdType>
        where TDto : class, IDtoWithId<TIdType>
        where TIdType : struct
    {
        /// <summary>Retrieves active and soft-deleted items asynchronously.</summary>
        Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves only soft-deleted items asynchronously.</summary>
        Task<IEnumerable<TDto>> GetAllDeletedAsync(
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves an item by its identifier, including soft-deleted items.</summary>
        Task<TDto> GetByIdIncludingDeletedAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        );
    }

    /// <summary>Defines DTO CRUD operations with restore and permanent-delete support.</summary>
    public interface ISoftDeleteCrudService<TDto, TIdType>
        : ICrudService<TDto, TIdType>,
            ISoftDeleteReadService<TDto, TIdType>
        where TDto : class, IDtoWithId<TIdType>
        where TIdType : struct
    {
        /// <summary>Restores a soft-deleted item asynchronously.</summary>
        Task RestoreAsync(TIdType id, CancellationToken cancellationToken = default);

        /// <summary>Permanently removes an item asynchronously.</summary>
        Task HardRemoveAsync(TIdType id, CancellationToken cancellationToken = default);
    }
}
