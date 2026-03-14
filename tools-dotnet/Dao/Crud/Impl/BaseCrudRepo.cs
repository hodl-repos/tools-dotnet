using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;
using tools_dotnet.Paging;
using tools_dotnet.Utility;

namespace tools_dotnet.Dao.Crud.Impl
{
    public abstract class BaseCrudRepo<TEntity, TIdType> : ICrudRepo<TEntity, TIdType>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
    {
        protected readonly DbContext _dbContext;
        protected readonly IPaginationProcessor _paginationProcessor;
        protected readonly IMapper _mapper;

        protected BaseCrudRepo(
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

        public virtual async Task<TIdType> AddAsync(
            TEntity item,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                await _dbContext.AddAsync(item, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return item.Id;
            }
            catch (DbUpdateException ex)
            {
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, false);
                throw;
            }
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>()).ToListAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllIncludingDeletedAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(
                _dbContext.Set<TEntity>(),
                SoftDeleteQueryMode.IncludeDeleted
            ).ToListAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> filters,
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>())
                .Where(filters)
                .ToListAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllIncludingDeletedAsync(
            Expression<Func<TEntity, bool>> filters,
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.IncludeDeleted
                )
                .Where(filters)
                .ToListAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllDeletedAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(
                _dbContext.Set<TEntity>(),
                SoftDeleteQueryMode.DeletedOnly
            ).ToListAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllDeletedAsync(
            Expression<Func<TEntity, bool>> filters,
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.DeletedOnly
                )
                .Where(filters)
                .ToListAsync(cancellationToken);
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>()).AsNoTracking();

            return await query.SortFilterAndPageAsync(
                apiPagination,
                _paginationProcessor,
                cancellationToken: cancellationToken
            );
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>())
                .Where(filter)
                .AsNoTracking();

            return await query.SortFilterAndPageAsync(
                apiPagination,
                _paginationProcessor,
                cancellationToken: cancellationToken
            );
        }

        public virtual async Task<TEntity?> FindAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            bool ignoreDeletedWithAuditable = true,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    ignoreDeletedWithAuditable
                )
                .Where(filter);

            if (throwOnMultipleFound)
            {
                return await query.SingleOrDefaultAsync(cancellationToken);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<TEntity> GetByIdAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            return await GetByIdInternalAsync(id, cancellationToken: cancellationToken);
        }

        public virtual async Task<TEntity> GetByIdIncludingDeletedAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            return await GetByIdInternalAsync(id, false, cancellationToken);
        }

        protected async Task<TEntity> GetByIdInternalAsync(
            TIdType id,
            bool ignoreDeletedWithAuditable = true,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    ignoreDeletedWithAuditable
                )
                .FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);

            if (entity == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), id);
            }

            return entity;
        }

        public virtual async Task UpdateAsync(
            TEntity item,
            CancellationToken cancellationToken = default
        )
        {
            var dbEntity = await GetByIdInternalAsync(item.Id, cancellationToken: cancellationToken);
            _mapper.Map(item, dbEntity);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, false);
                throw;
            }
        }

        public virtual async Task RemoveAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await GetByIdInternalAsync(id, cancellationToken: cancellationToken);

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
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, true);
                throw;
            }
        }

        public virtual async Task RestoreAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await GetByIdInternalAsync(id, false, cancellationToken);

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
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, false);
                throw;
            }
        }

        public virtual async Task HardRemoveAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await GetByIdInternalAsync(id, false, cancellationToken);
            _dbContext.Remove(entity);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, true);
                throw;
            }
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
