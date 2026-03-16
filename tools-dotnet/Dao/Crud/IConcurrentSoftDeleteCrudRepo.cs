using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;

namespace tools_dotnet.Dao.Crud
{
    public interface IConcurrentSoftDeleteCrudRepo<TEntity, TIdType, TConcurrencyToken>
        : IConcurrentCrudRepo<TEntity, TIdType, TConcurrencyToken>,
            ISoftDeleteReadRepo<TEntity, TIdType>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
    {
        Task RestoreAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        Task HardRemoveAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );
    }
}
