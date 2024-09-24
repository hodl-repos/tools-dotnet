using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sieve.Services;
using System.Linq.Expressions;
using tools_dotnet.Dao.Entity;
using System;
using System.Threading.Tasks;
using tools_dotnet.Exceptions;
using System.Collections.Generic;
using tools_dotnet.Paging;
using tools_dotnet.Utility;
using System.Linq;

namespace tools_dotnet.Dao.Crud.Impl
{
    public abstract class BaseCrudRepo<TEntity, TIdType> : ICrudRepo<TEntity, TIdType>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
    {
        protected readonly DbContext _dbContext;
        protected readonly ISieveProcessor _sieveProcessor;
        protected readonly IMapper _mapper;

        protected BaseCrudRepo(DbContext dbContext, IMapper mapper, ISieveProcessor sieveProcessor)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _sieveProcessor = sieveProcessor ?? throw new ArgumentNullException(nameof(sieveProcessor));
        }

        public virtual async Task<TIdType> AddAsync(TEntity item)
        {
            try
            {
                await _dbContext.AddAsync(item);
                await _dbContext.SaveChangesAsync();

                return item.Id;
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

        public virtual async Task<TEntity> GetByIdAsync(TIdType id)
        {
            return await GetByIdInternalAsync(id, false);
        }

        protected async Task<TEntity> GetByIdInternalAsync(TIdType id, bool ignoreDeletedWithAuditable = true)
        {
            var entity = await SetupQueryModifications(_dbContext.Set<TEntity>(), ignoreDeletedWithAuditable)
                .FirstOrDefaultAsync(x => x.Id.Equals(id));

            if (entity == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), id);
            }

            return entity;
        }

        public virtual async Task UpdateAsync(TEntity item)
        {
            var dbEntity = await GetByIdInternalAsync(item.Id);

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

        public virtual async Task RemoveAsync(TIdType id)
        {
            var entity = await GetByIdInternalAsync(id);

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