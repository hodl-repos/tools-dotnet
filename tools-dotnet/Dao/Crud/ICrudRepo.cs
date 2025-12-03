using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Dto;

namespace tools_dotnet.Dao.Crud
{
    public interface ICrudRepo<TEntity, TIdType> : ISortFilterAndPageRepo<TEntity>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
    {
        Task<TIdType> AddAsync(TEntity item);

        Task<IEnumerable<TEntity>> GetAllAsync();

        Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> filters);

        Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> filter, bool throwOnMultipleFound = true, bool ignoreDeletedWithAuditable = true);

        Task<TEntity> GetByIdAsync(TIdType id);

        Task UpdateAsync(TEntity item);

        Task RemoveAsync(TIdType id);
    }
}