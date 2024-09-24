using System.Linq.Expressions;
using tools_dotnet.Dao.Paging;
using System.Threading.Tasks;
using System.Collections.Generic;
using tools_dotnet.Dao.Entity;
using System;
using tools_dotnet.Dto;

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
        Task<TIdType> AddAsync(TInputDto item);

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