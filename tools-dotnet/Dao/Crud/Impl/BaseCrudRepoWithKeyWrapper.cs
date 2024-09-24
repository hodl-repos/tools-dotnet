using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using trace.api.Exceptions;
using trace.api.Dao.Context;
using trace.api.Dao.Entities.Base;
using Sieve.Services;
using System.Linq.Expressions;
using trace.api.Paging;
using trace.api.Sieve.Filters.Interfaces;
using trace.api.Util;
using trace.api.Util.Keys;

namespace trace.api.Dao.Repos.Crud.Impl
{
    public abstract class BaseCrudRepoWithKeyWrapper<TEntity, TKeyWrapper> : ICrudRepoWithKeyWrapper<TEntity, TKeyWrapper>
        where TEntity : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        protected readonly PostgresDbContext _dbContext;
        protected readonly ISieveProcessor _sieveProcessor;
        protected readonly IMapper _mapper;

        protected BaseCrudRepoWithKeyWrapper(PostgresDbContext dbContext, IMapper mapper, ISieveProcessor sieveProcessor)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sieveProcessor = sieveProcessor ?? throw new ArgumentNullException(nameof(sieveProcessor));
        }

        public virtual async Task<TEntity> AddAsync(TEntity item)
        {
            try
            {
                await _dbContext.AddAsync(item);
                await _dbContext.SaveChangesAsync();

                return item;
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

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>()).ToListAsync();
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> filters)
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>()).Where(filters).ToListAsync();
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(IApiSieve apiSieve)
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>()).AsNoTracking();

            return await query.SortFilterAndPageAsync(apiSieve, _sieveProcessor);
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(IApiSieve apiSieve, Expression<Func<TEntity, bool>> filter)
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>()).Where(filter).AsNoTracking();

            return await query.SortFilterAndPageAsync(apiSieve, _sieveProcessor);
        }

        public virtual async Task<TEntity> GetByIdAsync(TKeyWrapper keyWrapper)
        {
            return await GetByIdInternalAsync(keyWrapper, false);
        }

        public virtual async Task UpdateAsync(TKeyWrapper keyWrapper, TEntity item)
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

        public virtual async Task RemoveAsync(TKeyWrapper keyWrapper)
        {
            var entity = await GetByIdInternalAsync(keyWrapper);

            if (entity is IAuditableEntity auditableEntity)
            {
                auditableEntity.DeletedTimestamp = DateTimeOffset.UtcNow;
                _dbContext.Attach(auditableEntity);
                _dbContext.Entry(auditableEntity).State = EntityState.Modified;
            }
            else
            {
                _dbContext.Remove(entity);
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // foreign key violation
                if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.ForeignKeyViolation)
                {
                    throw new DependentItemException(pgEx.Message, true);
                }

                throw;
            }
        }

        protected virtual async Task<TEntity> GetByIdInternalAsync(TKeyWrapper keyWrapper, bool ignoreDeletedWithAuditable = true)
        {
            var entity = await SetupQueryModifications(_dbContext.Set<TEntity>(), ignoreDeletedWithAuditable)
                .FirstOrDefaultAsync(keyWrapper.GetKeyFilter());

            if (entity == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), keyWrapper.GetKeyAsString());
            }

            return entity;
        }

        protected virtual IQueryable<TEntity> SetupQueryModifications(IQueryable<TEntity> query, bool ignoreDeletedWithAuditable = true)
        {
            if (ignoreDeletedWithAuditable)
            {
                query = HandleAuditableEntity(query);
            }

            return query;
        }

        protected virtual IQueryable<TEntity> HandleAuditableEntity(IQueryable<TEntity> query)
        {
            if (query is IQueryable<IAuditableEntity> auditableQuery)
            {
                return auditableQuery.Where(e => e.DeletedTimestamp == null).Cast<TEntity>();
            }

            return query;
        }
    }
}