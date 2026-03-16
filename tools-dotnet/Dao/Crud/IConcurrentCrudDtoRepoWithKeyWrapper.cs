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
        Task<TKeyWrapper> AddAsync(
            TKeyWrapper keyWrapper,
            TInputDto item,
            CancellationToken cancellationToken = default
        );

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

        Task<TDto> GetByIdDtoAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TInputDto item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );
    }

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
        where TDto : class { }
}
