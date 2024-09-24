using trace.api.Dao.Entities.Base;
using trace.api.Dao.Repos.Paging;

namespace tools_dotnet.Dao.Crud
{
    public interface ICrudRepo<TEntity, TIdType> : ISortFilterAndPageRepo<TEntity>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
    {
        Task<TEntity> AddAsync(TEntity item);

        Task<IEnumerable<TEntity>> GetAllAsync();

        Task<TEntity> GetByIdAsync(TIdType id);

        Task UpdateAsync(TEntity item);

        Task RemoveAsync(TIdType id);
    }
}