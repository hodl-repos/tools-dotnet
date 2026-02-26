using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Dto;

namespace tools_dotnet.Dao.Crud
{
    public interface ICrudDtoRepo<TEntity, TIdType, TDto, TInputDto>
        : ICrudRepo<TEntity, TIdType>,
            ISortFilterAndPageDtoRepo<TEntity, TDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TInputDto : IDtoWithId<TIdType>
    {
        Task<TIdType> AddAsync(TInputDto item);

        Task<IEnumerable<TDto>> GetAllDtoAsync();

        Task<IEnumerable<TDto>> GetAllDtoAsync(Expression<Func<TEntity, bool>> filter);

        Task<TDto?> FindDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            bool ignoreDeletedWithAuditable = true
        );

        Task UpdateAsync(TInputDto item);

        Task<TDto> GetByIdDtoAsync(TIdType id);
    }

    public interface ICrudDtoRepo<TEntity, TIdType, TDto>
        : ICrudDtoRepo<TEntity, TIdType, TDto, TDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType> { }
}
