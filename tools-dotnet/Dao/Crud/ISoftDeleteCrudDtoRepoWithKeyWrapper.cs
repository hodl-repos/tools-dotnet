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
    public interface ISoftDeleteReadDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TEntity : class, IAuditableEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
    {
        Task<IEnumerable<TDto>> GetAllDtoAsync(
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        Task<IEnumerable<TDto>> GetAllDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );

        Task<TDto> GetByIdDtoAsync(
            TKeyWrapper keyWrapper,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        );
    }

    public interface ISoftDeleteCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TInputDto>
        : ICrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TInputDto>,
            ISoftDeleteCrudRepoWithKeyWrapper<TEntity, TKeyWrapper>,
            ISoftDeleteReadDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TEntity : class, IAuditableEntity, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
        where TInputDto : class { }

    public interface ISoftDeleteCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        : ISoftDeleteCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TDto>,
            ICrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TEntity : class, IAuditableEntity, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class { }
}
