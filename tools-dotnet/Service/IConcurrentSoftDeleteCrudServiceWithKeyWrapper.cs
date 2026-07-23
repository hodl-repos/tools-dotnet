using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;

namespace tools_dotnet.Service
{
    /// <summary>Defines concurrency-aware key-wrapper DTO CRUD operations for soft-deletable items.</summary>
    public interface IConcurrentSoftDeleteCrudServiceWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TConcurrencyToken
    > : IConcurrentCrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto, TConcurrencyToken>,
            ISoftDeleteReadServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TDto : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        /// <summary>Restores a soft-deleted item asynchronously.</summary>
        Task RestoreAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        /// <summary>Permanently removes an item asynchronously.</summary>
        Task HardRemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );
    }
}
