using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;
using tools_dotnet.Paging;
using tools_dotnet.Utility;

namespace tools_dotnet.Dao.Crud.Impl
{
    /// <summary>Provides a key-wrapper EF Core DTO repository base with soft delete.</summary>
    public abstract class BaseSoftDeleteCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TInputDto>
        : BaseCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TInputDto>,
            ISoftDeleteCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TInputDto>
        where TEntity : class, IAuditableEntity, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
        where TInputDto : class
    {
        /// <summary>Initializes a new instance of <c>BaseSoftDeleteCrudDtoRepoWithKeyWrapper</c>.</summary>
        protected BaseSoftDeleteCrudDtoRepoWithKeyWrapper(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor
        )
            : base(dbContext, mapper, paginationProcessor) { }

        /// <summary>Retrieves all available items asynchronously.</summary>
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .ToListAsync(cancellationToken);
        }

        /// <summary>Retrieves all available items asynchronously.</summary>
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

        /// <summary>Retrieves all available items asynchronously.</summary>
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

        /// <summary>Retrieves all available items asynchronously.</summary>
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

        /// <summary>Retrieves an item by its identifier asynchronously.</summary>
        public virtual async Task<TEntity> GetByIdAsync(
            TKeyWrapper keyWrapper,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        ) => await GetByIdInternalAsync(keyWrapper, softDeleteQueryMode, cancellationToken);

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
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

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        public virtual async Task<IEnumerable<TDto>> GetAllDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .Where(filter)
                .AsNoTracking()
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
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

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), softDeleteQueryMode)
                .Where(filter)
                .AsNoTracking();

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(
                apiPagination,
                _paginationProcessor,
                _mapper,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>Retrieves an item by its identifier and projects it to a DTO asynchronously.</summary>
        public virtual async Task<TDto> GetByIdDtoAsync(
            TKeyWrapper keyWrapper,
            SoftDeleteQueryMode softDeleteQueryMode = SoftDeleteQueryMode.ActiveOnly,
            CancellationToken cancellationToken = default
        )
        {
            var dto = await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    softDeleteQueryMode
                )
                .AsNoTracking()
                .Where(keyWrapper.GetKeyFilter())
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (dto == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), keyWrapper.GetKeyAsString());
            }

            return dto;
        }

        /// <summary>Restores a soft-deleted item asynchronously.</summary>
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

        /// <summary>Permanently removes an item asynchronously.</summary>
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

    /// <summary>Provides a key-wrapper EF Core DTO repository base with soft delete.</summary>
    public abstract class BaseSoftDeleteCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        : BaseSoftDeleteCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TDto>,
            ISoftDeleteCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TEntity : class, IAuditableEntity, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
    {
        /// <summary>Initializes a new instance of <c>BaseSoftDeleteCrudDtoRepoWithKeyWrapper</c>.</summary>
        protected BaseSoftDeleteCrudDtoRepoWithKeyWrapper(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor
        )
            : base(dbContext, mapper, paginationProcessor) { }
    }
}
