using trace.api.Dto.Base;
using trace.api.Dao.Entities.Base;
using trace.api.Dao.Repos.Paging;
using System.Linq.Expressions;
using trace.api.Util.Keys;

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
        Task<TDto> AddAsync(TInputDto item);

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