using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;

namespace tools_dotnet.Dao.Crud
{
    public interface IConcurrentCrudRepo<TEntity, TIdType, TConcurrencyToken>
        : ICrudRepo<TEntity, TIdType>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
    {
        Task UpdateAsync(TEntity item, TConcurrencyToken concurrencyToken);

        Task RemoveAsync(TIdType id, TConcurrencyToken concurrencyToken);

        Task RestoreAsync(TIdType id, TConcurrencyToken concurrencyToken);

        Task HardRemoveAsync(TIdType id, TConcurrencyToken concurrencyToken);

        Task<TConcurrencyToken> GetConcurrencyTokenAsync(TIdType id);
    }
}
