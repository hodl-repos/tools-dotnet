using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;

namespace tools_dotnet.Service
{
    public interface IConcurrentCrudServiceWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TConcurrencyToken
    > : ICrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TDto : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        Task UpdateAsync(TKeyWrapper keyWrapper, TDto item, TConcurrencyToken concurrencyToken);

        Task RemoveAsync(TKeyWrapper keyWrapper, TConcurrencyToken concurrencyToken);

        Task RestoreAsync(TKeyWrapper keyWrapper, TConcurrencyToken concurrencyToken);

        Task HardRemoveAsync(TKeyWrapper keyWrapper, TConcurrencyToken concurrencyToken);

        Task<TConcurrencyToken> GetConcurrencyTokenAsync(TKeyWrapper keyWrapper);
    }
}
