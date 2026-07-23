using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;

namespace tools_dotnet.Dao.Crud
{
    /// <summary>Defines concurrency-aware key-wrapper CRUD operations for soft-deletable entities.</summary>
    public interface IConcurrentSoftDeleteCrudRepoWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TConcurrencyToken
    > : IConcurrentCrudRepoWithKeyWrapper<TEntity, TKeyWrapper, TConcurrencyToken>,
            ISoftDeleteReadRepoWithKeyWrapper<TEntity, TKeyWrapper>
        where TEntity : class, IAuditableEntity
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
