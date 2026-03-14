using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dao.Paging;

namespace tools_dotnet.Dao.Crud
{
    public interface ICrudRepoWithKeyWrapper<TEntity, TKeyWrapper> : ISortFilterAndPageRepo<TEntity>
        where TEntity : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        Task<TKeyWrapper> AddAsync(TKeyWrapper keyWrapper, TEntity item);

        Task<IEnumerable<TEntity>> GetAllAsync();

        Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> filters);

        Task<IEnumerable<TEntity>> GetAllIncludingDeletedAsync();

        Task<IEnumerable<TEntity>> GetAllIncludingDeletedAsync(
            Expression<Func<TEntity, bool>> filters
        );

        Task<IEnumerable<TEntity>> GetAllDeletedAsync();

        Task<IEnumerable<TEntity>> GetAllDeletedAsync(Expression<Func<TEntity, bool>> filters);

        Task<TEntity> GetByIdAsync(TKeyWrapper keyWrapper);

        Task<TEntity> GetByIdIncludingDeletedAsync(TKeyWrapper keyWrapper);

        Task UpdateAsync(TKeyWrapper keyWrapper, TEntity item);

        Task RemoveAsync(TKeyWrapper keyWrapper);

        Task RestoreAsync(TKeyWrapper keyWrapper);

        Task HardRemoveAsync(TKeyWrapper keyWrapper);
    }
}
