using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;

namespace tools_dotnet.Dao.Crud
{
    /// <summary>Defines concurrency-aware CRUD operations for soft-deletable entities.</summary>
    public interface IConcurrentSoftDeleteCrudRepo<TEntity, TIdType, TConcurrencyToken>
        : IConcurrentCrudRepo<TEntity, TIdType, TConcurrencyToken>,
            ISoftDeleteReadRepo<TEntity, TIdType>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
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
