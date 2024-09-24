﻿using AutoMapper;
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
using trace.api.Util.Keys;

namespace trace.api.Dao.Repos.Crud.Impl
{
    public abstract class BaseCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TInputDto> :
        BaseCrudRepoWithKeyWrapper<TEntity, TKeyWrapper>,
        ICrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TInputDto>
        where TEntity : class, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
        where TInputDto : class
    {
        protected BaseCrudDtoRepoWithKeyWrapper(PostgresDbContext dbContext, IMapper mapper, ISieveProcessor sieveProcessor)
            : base(dbContext, mapper, sieveProcessor)
        {
        }

        protected abstract TKeyWrapper GetKeyWrapper(TEntity entity);

        public virtual async Task<TDto> AddAsync(TInputDto item)
        {
            var entity = _mapper.Map<TEntity>(item);

            entity = await AddAsync(entity);

            return await GetByIdDtoAsync(GetKeyWrapper(entity));
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDtoAsync()
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>()).AsNoTracking()
                .ProjectTo<TDto>(_mapper.ConfigurationProvider).ToListAsync();
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDtoAsync(Expression<Func<TEntity, bool>> filter)
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>()).Where(filter).AsNoTracking()
                .ProjectTo<TDto>(_mapper.ConfigurationProvider).ToListAsync();
        }

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(IApiSieve apiSieve)
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>()).AsNoTracking();

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(apiSieve, _sieveProcessor, _mapper);
        }

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(IApiSieve apiSieve, Expression<Func<TEntity, bool>> filter)
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>()).Where(filter).AsNoTracking();

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(apiSieve, _sieveProcessor, _mapper);
        }

        public virtual async Task<TDto> GetByIdDtoAsync(TKeyWrapper keyWrapper)
        {
            var dto = await SetupQueryModifications(_dbContext.Set<TEntity>(), false).AsNoTracking().Where(keyWrapper.GetKeyFilter())
                .ProjectTo<TDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync();

            if (dto == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), keyWrapper.GetKeyAsString());
            }

            return dto;
        }

        public virtual async Task UpdateAsync(TKeyWrapper keyWrapper, TInputDto item)
        {
            var dbEntity = await GetByIdInternalAsync(keyWrapper);

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

    public abstract class BaseCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto> : BaseCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TDto>
        where TEntity : class, IEntity

        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
    {
        protected BaseCrudDtoRepoWithKeyWrapper(PostgresDbContext dbContext, IMapper mapper, ISieveProcessor sieveProcessor) : base(dbContext, mapper, sieveProcessor)
        {
        }
    }
}