using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sieve.Services;
using System.Linq.Expressions;
using tools_dotnet.Dao.KeyWrapper;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using tools_dotnet.Utility;
using tools_dotnet.Paging;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Exceptions;
using System.Linq;

namespace tools_dotnet.Dao.Crud.Impl
{
    public abstract class BaseCrudRepoWithKeyWrapper<TEntity, TKeyWrapper> : ICrudRepoWithKeyWrapper<TEntity, TKeyWrapper>
        where TEntity : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        protected readonly DbContext _dbContext;
        protected readonly ISieveProcessor _sieveProcessor;
        protected readonly IMapper _mapper;

        protected BaseCrudRepoWithKeyWrapper(DbContext dbContext, IMapper mapper, ISieveProcessor sieveProcessor)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sieveProcessor = sieveProcessor ?? throw new ArgumentNullException(nameof(sieveProcessor));
        }

        public virtual async Task<TKeyWrapper> AddAsync(TKeyWrapper keyWrapper, TEntity item)
        {
            try
            {
                keyWrapper.UpdateEntityWithContainingResource(item);

                await _dbContext.AddAsync(item);
                await _dbContext.SaveChangesAsync();

                keyWrapper.UpdateKeyWrapperByEntity(item);

                return keyWrapper;
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
            return await SetupQueryModifications(_dbContext.Set<TEntity>())
                .Where(filters)
                .ToListAsync();
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(IApiSieve apiSieve)
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>()).AsNoTracking();

            return await query.SortFilterAndPageAsync(apiSieve, _sieveProcessor);
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(IApiSieve apiSieve, Expression<Func<TEntity, bool>> filter)
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>())
                .Where(filter)
                .AsNoTracking();

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

            keyWrapper.UpdateEntityWithContainingResource(dbEntity);

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