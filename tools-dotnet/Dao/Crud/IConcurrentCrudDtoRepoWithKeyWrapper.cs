using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dao.Paging;
using tools_dotnet.Paging;

namespace tools_dotnet.Dao.Crud
{
    /// <summary>Defines concurrency-aware key-wrapper CRUD operations with DTO projection.</summary>
    public interface IConcurrentCrudDtoRepoWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TInputDto,
        TConcurrencyToken
    > : IConcurrentCrudRepoWithKeyWrapper<TEntity, TKeyWrapper, TConcurrencyToken>,
            ISortFilterAndPageDtoRepo<TEntity, TDto>
        where TEntity : class, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
        where TInputDto : class
    {
        /// <summary>Adds a new item asynchronously.</summary>
        Task<TKeyWrapper> AddAsync(
            TKeyWrapper keyWrapper,
            TInputDto item,
            CancellationToken cancellationToken = default
        );

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

        /// <summary>Retrieves an item by its identifier and projects it to a DTO asynchronously.</summary>
        Task<TDto> GetByIdDtoAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        /// <summary>Updates an existing item asynchronously.</summary>
        Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TInputDto item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );
    }

    /// <summary>Defines concurrency-aware key-wrapper CRUD operations with DTO projection.</summary>
    public interface IConcurrentCrudDtoRepoWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TConcurrencyToken
    > : IConcurrentCrudDtoRepoWithKeyWrapper<
            TEntity,
            TKeyWrapper,
            TDto,
            TDto,
            TConcurrencyToken
        >,
            ISortFilterAndPageDtoRepo<TEntity, TDto>
        where TEntity : class, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
    { }
}
