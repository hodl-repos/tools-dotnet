using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;

namespace tools_dotnet.Dao.Crud
{
    public interface IConcurrentSoftDeleteCrudRepoWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TConcurrencyToken
    > : IConcurrentCrudRepoWithKeyWrapper<TEntity, TKeyWrapper, TConcurrencyToken>,
            ISoftDeleteReadRepoWithKeyWrapper<TEntity, TKeyWrapper>
        where TEntity : class, IAuditableEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        Task RestoreAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        Task HardRemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );
    }
}
