using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using trace.api.Dto.Base;
using trace.api.Exceptions;
using trace.api.Paging;
using trace.api.Sieve.Filters.Interfaces;
using trace.api.Util;
using trace.api.Dao.Context;
using trace.api.Dao.Entities.Base;
using Sieve.Services;
using System.Linq.Expressions;

namespace trace.api.Dao.Repos.Crud.Impl
{
    public abstract class BaseCrudDtoRepo<TEntity, TIdType, TDto, TInputDto> :
        BaseCrudRepo<TEntity, TIdType>,
        ICrudDtoRepo<TEntity, TIdType, TDto, TInputDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TInputDto : IDtoWithId<TIdType>
    {
        protected BaseCrudDtoRepo(PostgresDbContext dbContext, IMapper mapper, ISieveProcessor sieveProcessor)
            : base(dbContext, mapper, sieveProcessor)
        {
        }

        public virtual async Task<TDto> AddAsync(TInputDto item)
        {
            var entity = _mapper.Map<TEntity>(item);

            entity = await AddAsync(entity);

            return await GetByIdDtoAsync(entity.Id);
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDtoAsync()
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>()).AsNoTracking()
                .ProjectTo<TDto>(_mapper.ConfigurationProvider).ToListAsync();
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDtoAsync(Expression<Func<TEntity, bool>> filter)
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>());

            return await query.Where(filter)
                .ProjectTo<TDto>(_mapper.ConfigurationProvider).ToListAsync();
        }

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(IApiSieve apiSieve)
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>()).AsNoTracking();

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(apiSieve, _sieveProcessor, _mapper);
        }

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(IApiSieve apiSieve, Expression<Func<TEntity, bool>> filter)
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>()).AsNoTracking().Where(filter);

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(apiSieve, _sieveProcessor, _mapper);
        }

        public virtual async Task<TDto> GetByIdDtoAsync(TIdType id)
        {
            var dto = await SetupQueryModifications(_dbContext.Set<TEntity>(), false).AsNoTracking().Where(e => e.Id.Equals(id))
                .ProjectTo<TDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync();

            if (dto == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), id);
            }

            return dto;
        }

        public virtual async Task UpdateAsync(TInputDto item)
        {
            var dbEntity = await GetByIdAsync(item.Id);

            _mapper.Map(item, dbEntity);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is PostgresException pgEx)
                {
                    switch (pgEx.SqlState)
                    {
                        case PostgresErrorCodes.ForeignKeyViolation:
                            throw new DependentItemException(pgEx.Message, false);
                        case PostgresErrorCodes.UniqueViolation:
                            throw new ConflictingItemException(pgEx.Message);
                    }
                }

                throw;
            }
        }
    }

    public abstract class BaseCrudDtoRepo<TEntity, TIdType, TDto> : BaseCrudDtoRepo<TEntity, TIdType, TDto, TDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
    {
        protected BaseCrudDtoRepo(PostgresDbContext dbContext, IMapper mapper, ISieveProcessor sieveProcessor) : base(dbContext, mapper, sieveProcessor)
        {
        }
    }
}