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
    public class BaseSortFilterAndPageDtoRepo<TEntity, TDto> : ISortFilterAndPageDtoRepo<TEntity, TDto>
        where TEntity : class
    {
        protected readonly DbContext _dbContext;
        protected readonly IMapper _mapper;
        protected readonly IPaginationProcessor _paginationProcessor;

        public BaseSortFilterAndPageDtoRepo(DbContext dbContext, IMapper mapper, IPaginationProcessor paginationProcessor)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _paginationProcessor = paginationProcessor ?? throw new ArgumentNullException(nameof(paginationProcessor));
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(IApiPagination apiPagination)
        {
            var query = _dbContext.Set<TEntity>().AsNoTracking();

            return await query.SortFilterAndPageAsync(apiPagination, _paginationProcessor);
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(IApiPagination apiPagination, Expression<Func<TEntity, bool>> filter)
        {
            var query = _dbContext.Set<TEntity>().Where(filter).AsNoTracking();

            return await query.SortFilterAndPageAsync(apiPagination, _paginationProcessor);
        }

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(IApiPagination apiPagination, Expression<Func<TEntity, bool>> filter)
        {
            var query = _dbContext.Set<TEntity>().AsNoTracking().Where(filter);

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(apiPagination, _paginationProcessor, _mapper);
        }

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(IApiPagination apiPagination)
        {
            var query = _dbContext.Set<TEntity>().AsNoTracking();

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(apiPagination, _paginationProcessor, _mapper);
        }
    }
}