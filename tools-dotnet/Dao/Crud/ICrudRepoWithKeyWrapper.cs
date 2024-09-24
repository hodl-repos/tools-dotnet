using System.Linq.Expressions;
using trace.api.Dao.Entities.Base;
using trace.api.Dao.Repos.Paging;
using trace.api.Util.Keys;

namespace trace.api.Dao.Repos.Crud
{
    public interface ICrudRepoWithKeyWrapper<TEntity, TKeyWrapper> : ISortFilterAndPageRepo<TEntity>
        where TEntity : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        Task<TEntity> AddAsync(TEntity item);

        Task<IEnumerable<TEntity>> GetAllAsync();

        Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> filters);

        Task<TEntity> GetByIdAsync(TKeyWrapper keyWrapper);

        Task UpdateAsync(TKeyWrapper keyWrapper, TEntity item);

        Task RemoveAsync(TKeyWrapper keyWrapper);
    }
}