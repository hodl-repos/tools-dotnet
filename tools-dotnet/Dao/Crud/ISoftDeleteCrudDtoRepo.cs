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
    /// <summary>Defines DTO queries that can include or select soft-deleted rows.</summary>
    public interface ISoftDeleteReadDtoRepo<TEntity, TIdType, TDto>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
    {
        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        Task<IEnumerable<TDto>> GetAllDtoAsync(
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        Task<IEnumerable<TDto>> GetAllDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        /// <summary>Finds a single matching item and projects it to a DTO asynchronously.</summary>
        Task<TDto?> FindDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        /// <summary>Retrieves an item by its identifier and projects it to a DTO asynchronously.</summary>
        Task<TDto> GetByIdDtoAsync(
            TIdType id,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );
    }

    /// <summary>Defines DTO CRUD operations with restore and permanent-delete support.</summary>
    public interface ISoftDeleteCrudDtoRepo<TEntity, TIdType, TDto, TInputDto>
        : ICrudDtoRepo<TEntity, TIdType, TDto, TInputDto>,
            ISoftDeleteCrudRepo<TEntity, TIdType>,
            ISoftDeleteReadDtoRepo<TEntity, TIdType, TDto>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TInputDto : IDtoWithId<TIdType>
    { }

    /// <summary>Defines DTO CRUD operations with restore and permanent-delete support.</summary>
    public interface ISoftDeleteCrudDtoRepo<TEntity, TIdType, TDto>
        : ISoftDeleteCrudDtoRepo<TEntity, TIdType, TDto, TDto>,
            ICrudDtoRepo<TEntity, TIdType, TDto>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
    { }
}
