using System.Threading.Tasks;
using tools_dotnet.Dto;

namespace tools_dotnet.Service
{
    /// <summary>Defines concurrency-aware DTO CRUD operations for soft-deletable items.</summary>
    public interface IConcurrentSoftDeleteCrudService<TDto, TIdType, TConcurrencyToken>
        : IConcurrentCrudService<TDto, TIdType, TConcurrencyToken>,
            ISoftDeleteReadService<TDto, TIdType>
        where TDto : class, IDtoWithId<TIdType>
        where TIdType : struct
    {
        /// <summary>Restores a soft-deleted item asynchronously.</summary>
        Task RestoreAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        /// <summary>Permanently removes an item asynchronously.</summary>
        Task HardRemoveAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );
    }
}
