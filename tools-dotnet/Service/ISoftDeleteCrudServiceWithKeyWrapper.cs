using System.Collections.Generic;
using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Paging;

namespace tools_dotnet.Service
{
    /// <summary>Defines key-wrapper DTO queries that expose active and soft-deleted items.</summary>
    public interface ISoftDeleteReadServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TDto : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        /// <summary>Retrieves active and soft-deleted items asynchronously.</summary>
        Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves only soft-deleted items asynchronously.</summary>
        Task<IEnumerable<TDto>> GetAllDeletedAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves an item by its identifier, including soft-deleted items.</summary>
        Task<TDto> GetByIdIncludingDeletedAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );
    }

    /// <summary>Defines key-wrapper DTO CRUD operations with restore and permanent-delete support.</summary>
    public interface ISoftDeleteCrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        : ICrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>,
            ISoftDeleteReadServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TDto : class
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
