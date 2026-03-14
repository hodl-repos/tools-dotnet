using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;

namespace tools_dotnet.Dao.Crud
{
    public interface IConcurrentCrudRepoWithKeyWrapper<TEntity, TKeyWrapper, TConcurrencyToken>
        : ICrudRepoWithKeyWrapper<TEntity, TKeyWrapper>
        where TEntity : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        Task UpdateAsync(TKeyWrapper keyWrapper, TEntity item, TConcurrencyToken concurrencyToken);

        Task RemoveAsync(TKeyWrapper keyWrapper, TConcurrencyToken concurrencyToken);

        Task<TConcurrencyToken> GetConcurrencyTokenAsync(TKeyWrapper keyWrapper);
    }
}
