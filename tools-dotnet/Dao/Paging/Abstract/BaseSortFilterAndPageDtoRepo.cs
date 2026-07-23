using AutoMapper;
using Microsoft.EntityFrameworkCore;
using tools_dotnet.Pagination.Services;
using System.Linq.Expressions;
using System;
using System.Threading.Tasks;
using tools_dotnet.Paging;
using System.Linq;
using tools_dotnet.Utility;

namespace tools_dotnet.Dao.Paging.Abstract
{
    /// <summary>Provides a repository base for filtered, sorted, and paged DTO projections.</summary>
    public class BaseSortFilterAndPageDtoRepo<TEntity, TDto> : ISortFilterAndPageDtoRepo<TEntity, TDto>
        where TEntity : class
    {
        /// <summary>Gets the EF Core context used by the repository.</summary>
        protected readonly DbContext _dbContext;
        /// <summary>Gets the mapper used for entity and DTO conversion.</summary>
        protected readonly IMapper _mapper;
        /// <summary>Gets the pagination processor applied to repository queries.</summary>
        protected readonly IPaginationProcessor _paginationProcessor;

        /// <summary>Initializes a new instance of <c>BaseSortFilterAndPageDtoRepo</c>.</summary>
        public BaseSortFilterAndPageDtoRepo(DbContext dbContext, IMapper mapper, IPaginationProcessor paginationProcessor)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _paginationProcessor = paginationProcessor ?? throw new ArgumentNullException(nameof(paginationProcessor));
        }

        /// <summary>Retrieves all available items asynchronously.</summary>
        public virtual async Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        )
        {
            var query = _dbContext.Set<TEntity>().AsNoTracking();

            return await query.SortFilterAndPageAsync(
                apiPagination,
                _paginationProcessor,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>Retrieves all available items asynchronously.</summary>
        public virtual async Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        )
        {
            var query = _dbContext.Set<TEntity>().Where(filter).AsNoTracking();

            return await query.SortFilterAndPageAsync(
                apiPagination,
                _paginationProcessor,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        )
        {
            var query = _dbContext.Set<TEntity>().AsNoTracking().Where(filter);

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(
                apiPagination,
                _paginationProcessor,
                _mapper,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        )
        {
            var query = _dbContext.Set<TEntity>().AsNoTracking();

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(
                apiPagination,
                _paginationProcessor,
                _mapper,
                cancellationToken: cancellationToken
            );
        }
    }
}
