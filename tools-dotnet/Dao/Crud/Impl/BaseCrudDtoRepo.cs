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
    /// <summary>Provides an EF Core CRUD repository base with AutoMapper DTO projection.</summary>
    public abstract class BaseCrudDtoRepo<TEntity, TIdType, TDto, TInputDto>
        : BaseCrudRepo<TEntity, TIdType>,
            ICrudDtoRepo<TEntity, TIdType, TDto, TInputDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TInputDto : IDtoWithId<TIdType>
    {
        /// <summary>Initializes a new instance of <c>BaseCrudDtoRepo</c>.</summary>
        protected BaseCrudDtoRepo(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor
        )
            : base(dbContext, mapper, paginationProcessor) { }

        /// <summary>Adds a new item asynchronously.</summary>
        public virtual async Task<TIdType> AddAsync(
            TInputDto item,
            CancellationToken cancellationToken = default
        )
        {
            var entity = _mapper.Map<TEntity>(item);

            return await AddAsync(entity, cancellationToken);
        }

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        public virtual async Task<IEnumerable<TDto>> GetAllDtoAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.ActiveOnly
                )
                .AsNoTracking()
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        public virtual async Task<IEnumerable<TDto>> GetAllDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), SoftDeleteQueryMode.ActiveOnly)
                .AsNoTracking();

            return await query
                .Where(filter)
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        /// <summary>Retrieves all available items projected to DTOs asynchronously.</summary>
        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        )
        {
            return await GetAllDtoInternalAsync(
                apiPagination,
                SoftDeleteQueryMode.ActiveOnly,
                cancellationToken
            );
        }

        /// <summary>Retrieves and projects items using the selected soft-delete query mode and filters.</summary>
        protected virtual async Task<IPagedList<TDto>> GetAllDtoInternalAsync(
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
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), SoftDeleteQueryMode.ActiveOnly)
                .AsNoTracking()
                .Where(filter);

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(
                apiPagination,
                _paginationProcessor,
                _mapper,
                cancellationToken: cancellationToken
            );
        }

        /// <summary>Finds a single matching item and projects it to a DTO asynchronously.</summary>
        public virtual async Task<TDto?> FindDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), SoftDeleteQueryMode.ActiveOnly)
                .AsNoTracking()
                .Where(filter)
                .ProjectTo<TDto>(_mapper.ConfigurationProvider);

            if (throwOnMultipleFound)
            {
                return await query.SingleOrDefaultAsync(cancellationToken);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>Retrieves an item by its identifier and projects it to a DTO asynchronously.</summary>
        public virtual async Task<TDto> GetByIdDtoAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            var dto = await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.ActiveOnly
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

        /// <summary>Updates an existing item asynchronously.</summary>
        public virtual async Task UpdateAsync(
            TInputDto item,
            CancellationToken cancellationToken = default
        )
        {
            var dbEntity = await GetByIdAsync(item.Id, cancellationToken);
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
    }

    /// <summary>Provides an EF Core CRUD repository base with AutoMapper DTO projection.</summary>
    public abstract class BaseCrudDtoRepo<TEntity, TIdType, TDto>
        : BaseCrudDtoRepo<TEntity, TIdType, TDto, TDto>,
            ICrudDtoRepo<TEntity, TIdType, TDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
    {
        /// <summary>Initializes a new instance of <c>BaseCrudDtoRepo</c>.</summary>
        protected BaseCrudDtoRepo(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor
        )
            : base(dbContext, mapper, paginationProcessor) { }
    }
}
