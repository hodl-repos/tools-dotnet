using trace.api.Dto.Base;
using trace.api.Dao.Entities.Base;
using trace.api.Dao.Repos.Paging;
using System.Linq.Expressions;

namespace tools_dotnet.Dao.Crud
{
    public interface ICrudDtoRepo<TEntity, TIdType, TDto, TInputDto> :
        ICrudRepo<TEntity, TIdType>,
        ISortFilterAndPageDtoRepo<TEntity, TDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TInputDto : IDtoWithId<TIdType>
    {
        Task<TDto> AddAsync(TInputDto item);

        Task<IEnumerable<TDto>> GetAllDtoAsync();

        Task<IEnumerable<TDto>> GetAllDtoAsync(Expression<Func<TEntity, bool>> filter);

        Task UpdateAsync(TInputDto item);

        Task<TDto> GetByIdDtoAsync(TIdType id);
    }

    public interface ICrudDtoRepo<TEntity, TIdType, TDto> : ICrudDtoRepo<TEntity, TIdType, TDto, TDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
    {
    }
}