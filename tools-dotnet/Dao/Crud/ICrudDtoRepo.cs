using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Dto;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Crud
{
    /// <summary>Defines CRUD operations with entity-to-DTO projection.</summary>
    public interface ICrudDtoRepo<TEntity, TIdType, TDto, TInputDto>
        : ICrudRepo<TEntity, TIdType>,
            ISortFilterAndPageDtoRepo<TEntity, TDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TInputDto : IDtoWithId<TIdType>
    {
        /// <summary>Adds a new item asynchronously.</summary>
        Task<TIdType> AddAsync(TInputDto item, CancellationToken cancellationToken = default);

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        Task<IEnumerable<TDto>> GetAllDtoAsync(CancellationToken cancellationToken = default);

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        Task<IEnumerable<TDto>> GetAllDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        new Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        new Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        );

        /// <summary>Finds a single matching item and projects it to a DTO asynchronously.</summary>
        Task<TDto?> FindDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            CancellationToken cancellationToken = default
        );

        /// <summary>Updates an existing item asynchronously.</summary>
        Task UpdateAsync(TInputDto item, CancellationToken cancellationToken = default);

        /// <summary>Retrieves an item by its identifier and projects it to a DTO asynchronously.</summary>
        Task<TDto> GetByIdDtoAsync(TIdType id, CancellationToken cancellationToken = default);
    }

    /// <summary>Defines CRUD operations with entity-to-DTO projection.</summary>
    public interface ICrudDtoRepo<TEntity, TIdType, TDto>
        : ICrudDtoRepo<TEntity, TIdType, TDto, TDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
    { }
}
