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
        Task<TIdType> AddAsync(TInputDto item, CancellationToken cancellationToken = default);

        Task<IEnumerable<TDto>> GetAllDtoAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<TDto>> GetAllDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        );

        Task<IEnumerable<TDto>> GetAllDtoIncludingDeletedAsync(
            CancellationToken cancellationToken = default
        );

        Task<IEnumerable<TDto>> GetAllDtoIncludingDeletedAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        );

        Task<IEnumerable<TDto>> GetAllDeletedDtoAsync(
            CancellationToken cancellationToken = default
        );

        Task<IEnumerable<TDto>> GetAllDeletedDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        );

        Task<TDto?> FindDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            bool ignoreDeletedWithAuditable = true,
            CancellationToken cancellationToken = default
        );

        Task UpdateAsync(TInputDto item, CancellationToken cancellationToken = default);

        Task<TDto> GetByIdDtoAsync(TIdType id, CancellationToken cancellationToken = default);

        Task<TDto> GetByIdDtoIncludingDeletedAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        );
    }

    public interface ICrudDtoRepo<TEntity, TIdType, TDto>
        : ICrudDtoRepo<TEntity, TIdType, TDto, TDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType> { }
}
