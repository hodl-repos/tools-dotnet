using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;

namespace tools_dotnet.Dao.Crud
{
    public interface IConcurrentCrudRepoWithKeyWrapper<TEntity, TKeyWrapper, TConcurrencyToken>
        : ICrudRepoWithKeyWrapper<TEntity, TKeyWrapper>
        where TEntity : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TEntity item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        Task RemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

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

        Task<TConcurrencyToken> GetConcurrencyTokenAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );
    }
}
