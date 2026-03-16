using System.Threading.Tasks;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;
using tools_dotnet.Paging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace tools_dotnet.Dao.Crud
{
    public interface IConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TInputDto, TConcurrencyToken>
        : IConcurrentCrudRepo<TEntity, TIdType, TConcurrencyToken>,
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

        Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        );

        Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        );

        Task<TDto?> FindDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            CancellationToken cancellationToken = default
        );

        Task<TDto> GetByIdDtoAsync(TIdType id, CancellationToken cancellationToken = default);

        Task UpdateAsync(
            TInputDto item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );
    }

    public interface IConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TConcurrencyToken>
        : IConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TDto, TConcurrencyToken>,
            ISortFilterAndPageDtoRepo<TEntity, TDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType> { }
}
