using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;
using tools_dotnet.Paging;
using tools_dotnet.Utility;

namespace tools_dotnet.Dao.Crud.Impl
{
    public abstract class BaseCrudRepoWithKeyWrapper<TEntity, TKeyWrapper>
        : ICrudRepoWithKeyWrapper<TEntity, TKeyWrapper>
        where TEntity : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        protected readonly DbContext _dbContext;
        protected readonly IPaginationProcessor _paginationProcessor;
        protected readonly IMapper _mapper;

        protected BaseCrudRepoWithKeyWrapper(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor
        )
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _paginationProcessor =
                paginationProcessor ?? throw new ArgumentNullException(nameof(paginationProcessor));
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
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, false);
                throw;
            }
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>()).ToListAsync();
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllIncludingDeletedAsync()
        {
            return await SetupQueryModifications(
                _dbContext.Set<TEntity>(),
                SoftDeleteQueryMode.IncludeDeleted
            ).ToListAsync();
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> filters
        )
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>())
                .Where(filters)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllIncludingDeletedAsync(
            Expression<Func<TEntity, bool>> filters
        )
        {
            return await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.IncludeDeleted
                )
                .Where(filters)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllDeletedAsync()
        {
            return await SetupQueryModifications(
                _dbContext.Set<TEntity>(),
                SoftDeleteQueryMode.DeletedOnly
            ).ToListAsync();
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllDeletedAsync(
            Expression<Func<TEntity, bool>> filters
        )
        {
            return await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.DeletedOnly
                )
                .Where(filters)
                .ToListAsync();
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(IApiPagination apiPagination)
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>()).AsNoTracking();

            return await query.SortFilterAndPageAsync(apiPagination, _paginationProcessor);
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>())
                .Where(filter)
                .AsNoTracking();

            return await query.SortFilterAndPageAsync(apiPagination, _paginationProcessor);
        }

        public virtual async Task<TEntity> GetByIdAsync(TKeyWrapper keyWrapper)
        {
            return await GetByIdInternalAsync(keyWrapper);
        }

        public virtual async Task<TEntity> GetByIdIncludingDeletedAsync(TKeyWrapper keyWrapper)
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
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, false);
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
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, true);
                throw;
            }
        }

        public virtual async Task RestoreAsync(TKeyWrapper keyWrapper)
        {
            var entity = await GetByIdInternalAsync(keyWrapper, false);

            if (entity is not IAuditableEntity auditableEntity)
            {
                throw CreateSoftDeleteNotSupportedException(nameof(RestoreAsync));
            }

            if (auditableEntity.DeletedTimestamp == null)
            {
                return;
            }

            auditableEntity.DeletedTimestamp = null;
            _dbContext.Attach(auditableEntity);
            _dbContext.Entry(auditableEntity).State = EntityState.Modified;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, false);
                throw;
            }
        }

        public virtual async Task HardRemoveAsync(TKeyWrapper keyWrapper)
        {
            var entity = await GetByIdInternalAsync(keyWrapper, false);
            _dbContext.Remove(entity);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, true);
                throw;
            }
        }

        protected virtual async Task<TEntity> GetByIdInternalAsync(
            TKeyWrapper keyWrapper,
            bool ignoreDeletedWithAuditable = true
        )
        {
            var entity = await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    ignoreDeletedWithAuditable
                )
                .FirstOrDefaultAsync(keyWrapper.GetKeyFilter());

            if (entity == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), keyWrapper.GetKeyAsString());
            }

            return entity;
        }

        protected virtual IQueryable<TEntity> SetupQueryModifications(
            IQueryable<TEntity> query,
            bool ignoreDeletedWithAuditable = true
        )
        {
            return SetupQueryModifications(
                query,
                ignoreDeletedWithAuditable
                    ? SoftDeleteQueryMode.ActiveOnly
                    : SoftDeleteQueryMode.IncludeDeleted
            );
        }

        protected virtual IQueryable<TEntity> SetupQueryModifications(
            IQueryable<TEntity> query,
            SoftDeleteQueryMode softDeleteQueryMode
        )
        {
            return HandleAuditableEntity(query, softDeleteQueryMode);
        }

        protected virtual IQueryable<TEntity> HandleAuditableEntity(
            IQueryable<TEntity> query,
            SoftDeleteQueryMode softDeleteQueryMode
        )
        {
            if (query is IQueryable<IAuditableEntity> auditableQuery)
            {
                return softDeleteQueryMode switch
                {
                    SoftDeleteQueryMode.ActiveOnly => auditableQuery
                        .Where(e => e.DeletedTimestamp == null)
                        .Cast<TEntity>(),
                    SoftDeleteQueryMode.IncludeDeleted => auditableQuery.Cast<TEntity>(),
                    SoftDeleteQueryMode.DeletedOnly => auditableQuery
                        .Where(e => e.DeletedTimestamp != null)
                        .Cast<TEntity>(),
                    _ => query,
                };
            }

            if (softDeleteQueryMode == SoftDeleteQueryMode.DeletedOnly)
            {
                return query.Where(_ => false);
            }

            return query;
        }

        protected virtual InvalidOperationException CreateSoftDeleteNotSupportedException(
            string operationName
        )
        {
            return new(
                $"Operation '{operationName}' requires '{typeof(TEntity).Name}' to implement "
                    + $"{nameof(IAuditableEntity)}."
            );
        }
    }
}
