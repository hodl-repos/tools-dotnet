using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;
using tools_dotnet.Paging;
using tools_dotnet.Utility;

namespace tools_dotnet.Dao.Crud.Impl
{
    public abstract class BaseConcurrentSoftDeleteCrudDtoRepo<
        TEntity,
        TIdType,
        TDto,
        TInputDto,
        TConcurrencyToken
    > : BaseConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TInputDto, TConcurrencyToken>,
            IConcurrentSoftDeleteCrudDtoRepo<
                TEntity,
                TIdType,
                TDto,
                TInputDto,
                TConcurrencyToken
            >
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TInputDto : IDtoWithId<TIdType>
    {
        protected BaseConcurrentSoftDeleteCrudDtoRepo(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor,
            CrudConcurrencyConfiguration concurrencyConfiguration
        )
            : base(dbContext, mapper, paginationProcessor, concurrencyConfiguration) { }

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

        public virtual async Task<TEntity?> FindAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .Where(filter);

            if (throwOnMultipleFound)
            {
                return await query.SingleOrDefaultAsync(cancellationToken);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<TEntity> GetByIdAsync(
            TIdType id,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        ) => await GetByIdInternalAsync(id, softDeleteQueryMode, cancellationToken);

        public virtual async Task<IEnumerable<TDto>> GetAllDtoAsync(
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .AsNoTracking()
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .AsNoTracking();

            return await query
                .Where(filter)
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .AsNoTracking();

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(
                apiPagination,
                _paginationProcessor,
                _mapper,
                cancellationToken: cancellationToken
            );
        }

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .AsNoTracking()
                .Where(filter);

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(
                apiPagination,
                _paginationProcessor,
                _mapper,
                cancellationToken: cancellationToken
            );
        }

        public virtual async Task<TDto?> FindDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .AsNoTracking()
                .Where(filter)
                .ProjectTo<TDto>(_mapper.ConfigurationProvider);

            if (throwOnMultipleFound)
            {
                return await query.SingleOrDefaultAsync(cancellationToken);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<TDto> GetByIdDtoAsync(
            TIdType id,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            var dto = await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    softDeleteQueryMode
                )
                .AsNoTracking()
                .Where(e => e.Id.Equals(id))
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (dto == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), id);
            }

            return dto;
        }

        public virtual async Task RestoreAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await GetByIdInternalAsync(
                id,
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );
            CrudConcurrencyHelper.EnsureMatchingConcurrencyTokenValue(
                _concurrencyConfiguration,
                entity,
                concurrencyToken
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

        public virtual async Task HardRemoveAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await GetByIdInternalAsync(
                id,
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );
            CrudConcurrencyHelper.EnsureMatchingConcurrencyTokenValue(
                _concurrencyConfiguration,
                entity,
                concurrencyToken
            );

            _dbContext.Remove(entity);

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
    }

    public abstract class BaseConcurrentSoftDeleteCrudDtoRepo<
        TEntity,
        TIdType,
        TDto,
        TConcurrencyToken
    > : BaseConcurrentSoftDeleteCrudDtoRepo<TEntity, TIdType, TDto, TDto, TConcurrencyToken>,
            IConcurrentSoftDeleteCrudDtoRepo<TEntity, TIdType, TDto, TConcurrencyToken>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
    {
        protected BaseConcurrentSoftDeleteCrudDtoRepo(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor,
            CrudConcurrencyConfiguration concurrencyConfiguration
        )
            : base(dbContext, mapper, paginationProcessor, concurrencyConfiguration) { }
    }
}
