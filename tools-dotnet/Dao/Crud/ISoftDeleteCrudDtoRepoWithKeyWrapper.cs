using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Crud
{
    /// <summary>Defines key-wrapper DTO queries that can include soft-deleted rows.</summary>
    public interface ISoftDeleteReadDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TEntity : class, IAuditableEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
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

        /// <summary>Retrieves an item by its identifier and projects it to a DTO asynchronously.</summary>
        Task<TDto> GetByIdDtoAsync(
            TKeyWrapper keyWrapper,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );
    }

    /// <summary>Defines key-wrapper DTO CRUD operations with restore and permanent-delete support.</summary>
    public interface ISoftDeleteCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TInputDto>
        : ICrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TInputDto>,
            ISoftDeleteCrudRepoWithKeyWrapper<TEntity, TKeyWrapper>,
            ISoftDeleteReadDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TEntity : class, IAuditableEntity, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
        where TInputDto : class
    { }

    /// <summary>Defines key-wrapper DTO CRUD operations with restore and permanent-delete support.</summary>
    public interface ISoftDeleteCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        : ISoftDeleteCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TDto>,
            ICrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TEntity : class, IAuditableEntity, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
    { }
}
