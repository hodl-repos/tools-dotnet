using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dao.Paging;

namespace tools_dotnet.Dao.Crud
{
    public interface ICrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TInputDto> :
        ICrudRepoWithKeyWrapper<TEntity, TKeyWrapper>,
        ISortFilterAndPageDtoRepo<TEntity, TDto>
        where TEntity : class, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
        where TInputDto : class
    {
        Task<TKeyWrapper> AddAsync(TKeyWrapper keyWrapper, TInputDto item);

        Task<IEnumerable<TDto>> GetAllDtoAsync();

        Task<IEnumerable<TDto>> GetAllDtoAsync(Expression<Func<TEntity, bool>> filter);

        Task UpdateAsync(TKeyWrapper keyWrapper, TInputDto item);

        Task<TDto> GetByIdDtoAsync(TKeyWrapper keyWrapper);
    }

    public interface ICrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto> : ICrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TDto>
        where TEntity : class, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
    {
    }
}