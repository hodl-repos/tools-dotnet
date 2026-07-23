using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;
using tools_dotnet.Paging;
using tools_dotnet.Time;
using tools_dotnet.Utility;

namespace tools_dotnet.Dao.Crud.Impl
{
    /// <summary>Provides an EF Core CRUD repository base with optimistic concurrency.</summary>
    public abstract class BaseConcurrentCrudRepo<TEntity, TIdType, TConcurrencyToken>
        : IConcurrentCrudRepo<TEntity, TIdType, TConcurrencyToken>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
    {
        /// <summary>Gets the EF Core context used by the repository.</summary>
        protected readonly DbContext _dbContext;
        /// <summary>Gets the pagination processor applied to repository queries.</summary>
        protected readonly IPaginationProcessor _paginationProcessor;
        /// <summary>Gets the mapper used for entity and DTO conversion.</summary>
        protected readonly IMapper _mapper;
        /// <summary>Gets the configuration used to read and compare concurrency tokens.</summary>
        protected readonly CrudConcurrencyConfiguration _concurrencyConfiguration;
        /// <summary>Gets the clock used to generate UTC timestamps.</summary>
        protected readonly IClockProvider _clockProvider;

        /// <summary>Initializes a new instance of <c>BaseConcurrentCrudRepo</c>.</summary>
        protected BaseConcurrentCrudRepo(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor,
            CrudConcurrencyConfiguration concurrencyConfiguration,
            IClockProvider? clockProvider = null
        )
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _paginationProcessor =
                paginationProcessor ?? throw new ArgumentNullException(nameof(paginationProcessor));
            _concurrencyConfiguration =
                concurrencyConfiguration
                ?? throw new ArgumentNullException(nameof(concurrencyConfiguration));
            _clockProvider = clockProvider ?? SystemClockProvider.Instance;
        }

        /// <summary>Adds a new item asynchronously.</summary>
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

        /// <summary>Retrieves all available items asynchronously.</summary>
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.ActiveOnly
                )
                .ToListAsync(cancellationToken);
        }

        /// <summary>Retrieves all available items asynchronously.</summary>
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> filters,
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.ActiveOnly
                )
                .Where(filters)
                .ToListAsync(cancellationToken);
        }

        /// <summary>Retrieves all available items asynchronously.</summary>
        public virtual async Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        )
        {
            return await GetAllInternalAsync(
                apiPagination,
                SoftDeleteQueryMode.ActiveOnly,
                cancellationToken
            );
        }

        /// <summary>Retrieves items using the selected soft-delete query mode and filters.</summary>
        protected virtual async Task<IPagedList<TEntity>> GetAllInternalAsync(
            IApiPagination apiPagination,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .AsNoTracking();

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
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), SoftDeleteQueryMode.ActiveOnly)
                .Where(filter)
                .AsNoTracking();

            return await query.SortFilterAndPageAsync(
                apiPagination,
                _paginationProcessor,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>Finds a single item that matches a predicate asynchronously.</summary>
        public virtual async Task<TEntity?> FindAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), SoftDeleteQueryMode.ActiveOnly)
                .Where(filter);

            if (throwOnMultipleFound)
            {
                return await query.SingleOrDefaultAsync(cancellationToken);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>Retrieves an item by its identifier asynchronously.</summary>
        public virtual async Task<TEntity> GetByIdAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        ) => await GetByIdInternalAsync(id, SoftDeleteQueryMode.ActiveOnly, cancellationToken);

        /// <summary>Retrieves an item by its identifier using the selected soft-delete query mode.</summary>
        protected async Task<TEntity> GetByIdInternalAsync(
            TIdType id,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    softDeleteQueryMode
                )
                .FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);

            if (entity == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), id);
            }

            return entity;
        }

        /// <summary>Updates an existing item asynchronously.</summary>
        public virtual async Task UpdateAsync(
            TEntity item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            var dbEntity = await GetByIdInternalAsync(item.Id, cancellationToken: cancellationToken);
            CrudConcurrencyHelper.EnsureMatchingConcurrencyTokenValue(
                _concurrencyConfiguration,
                dbEntity,
                concurrencyToken
            );

            _mapper.Map(item, dbEntity);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw await CrudConcurrencyHelper.CreateConcurrentModificationExceptionAsync(
                    _concurrencyConfiguration,
                    ex,
                    concurrencyToken,
                    cancellationToken
                );
            }
            catch (DbUpdateException ex)
            {
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, false);
                throw;
            }
        }

        /// <summary>Removes an item asynchronously.</summary>
        public virtual async Task RemoveAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await GetByIdInternalAsync(id, cancellationToken: cancellationToken);
            CrudConcurrencyHelper.EnsureMatchingConcurrencyTokenValue(
                _concurrencyConfiguration,
                entity,
                concurrencyToken
            );

            if (entity is IAuditableEntity auditableEntity)
            {
                auditableEntity.DeletedTimestamp = _clockProvider.UtcNow;
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
            catch (DbUpdateConcurrencyException ex)
            {
                throw await CrudConcurrencyHelper.CreateConcurrentModificationExceptionAsync(
                    _concurrencyConfiguration,
                    ex,
                    concurrencyToken,
                    cancellationToken
                );
            }
            catch (DbUpdateException ex)
            {
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, true);
                throw;
            }
        }

        /// <summary>Retrieves the current concurrency token for an item asynchronously.</summary>
        public virtual async Task<TConcurrencyToken> GetConcurrencyTokenAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.IncludeDeleted
                )
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);

            if (entity == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), id);
            }

            return CrudConcurrencyHelper.GetRequiredPersistedConcurrencyToken<TConcurrencyToken>(
                _concurrencyConfiguration,
                entity
            );
        }

        /// <summary>Applies repository-specific modifications to a query.</summary>
        protected virtual IQueryable<TEntity> SetupQueryModifications(
            IQueryable<TEntity> query,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly
        )
        {
            return HandleAuditableEntity(query, softDeleteQueryMode);
        }

        /// <summary>Applies audit timestamp and soft-delete state changes to an entity.</summary>
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

        /// <summary>Creates an exception that reports missing soft-delete support.</summary>
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
