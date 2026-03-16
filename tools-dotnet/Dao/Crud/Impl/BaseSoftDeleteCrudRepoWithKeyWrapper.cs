using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Pagination.Services;
using tools_dotnet.Paging;
using tools_dotnet.Utility;

namespace tools_dotnet.Dao.Crud.Impl
{
    public abstract class BaseSoftDeleteCrudRepoWithKeyWrapper<TEntity, TKeyWrapper>
        : BaseCrudRepoWithKeyWrapper<TEntity, TKeyWrapper>,
            ISoftDeleteCrudRepoWithKeyWrapper<TEntity, TKeyWrapper>
        where TEntity : class, IAuditableEntity, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        protected BaseSoftDeleteCrudRepoWithKeyWrapper(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor
        )
            : base(dbContext, mapper, paginationProcessor) { }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .ToListAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>> filters,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .Where(filters)
                .ToListAsync(cancellationToken);
        }

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(
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

        public virtual async Task<IPagedList<TEntity>> GetAllAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .Where(filter)
                .AsNoTracking();

            return await query.SortFilterAndPageAsync(
                apiPagination,
                _paginationProcessor,
                cancellationToken: cancellationToken
            );
        }

        public virtual async Task<TEntity> GetByIdAsync(
            TKeyWrapper keyWrapper,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        ) => await GetByIdInternalAsync(keyWrapper, softDeleteQueryMode, cancellationToken);

        public virtual async Task RestoreAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await GetByIdInternalAsync(
                keyWrapper,
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );

            if (entity.DeletedTimestamp == null)
            {
                return;
            }

            entity.DeletedTimestamp = null;
            _dbContext.Attach(entity);
            _dbContext.Entry(entity).State = EntityState.Modified;

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
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await GetByIdInternalAsync(
                keyWrapper,
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );
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
    }
}
