using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sieve.Services;
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
        protected readonly ISieveProcessor _sieveProcessor;

        public BaseSortFilterAndPageDtoRepo(DbContext dbContext, IMapper mapper, ISieveProcessor sieveProcessor)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sieveProcessor = sieveProcessor ?? throw new ArgumentNullException(nameof(sieveProcessor));
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(IApiSieve apiSieve)
        {
            var query = _dbContext.Set<TEntity>().AsNoTracking();

            return await query.SortFilterAndPageAsync(apiSieve, _sieveProcessor);
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(IApiSieve apiSieve, Expression<Func<TEntity, bool>> filter)
        {
            var query = _dbContext.Set<TEntity>().Where(filter).AsNoTracking();

            return await query.SortFilterAndPageAsync(apiSieve, _sieveProcessor);
        }

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(IApiSieve apiSieve, Expression<Func<TEntity, bool>> filter)
        {
            var query = _dbContext.Set<TEntity>().AsNoTracking().Where(filter);

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(apiSieve, _sieveProcessor, _mapper);
        }

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(IApiSieve apiSieve)
        {
            var query = _dbContext.Set<TEntity>().AsNoTracking();

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(apiSieve, _sieveProcessor, _mapper);
        }
    }
}