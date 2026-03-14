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
    public abstract class BaseCrudDtoRepo<TEntity, TIdType, TDto, TInputDto>
        : BaseCrudRepo<TEntity, TIdType>,
            ICrudDtoRepo<TEntity, TIdType, TDto, TInputDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TInputDto : IDtoWithId<TIdType>
    {
        protected BaseCrudDtoRepo(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor
        )
            : base(dbContext, mapper, paginationProcessor) { }

        public virtual async Task<TIdType> AddAsync(TInputDto item)
        {
            var entity = _mapper.Map<TEntity>(item);

            return await AddAsync(entity);
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDtoAsync()
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>())
                .AsNoTracking()
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDtoIncludingDeletedAsync()
        {
            return await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.IncludeDeleted
                )
                .AsNoTracking()
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDtoAsync(
            Expression<Func<TEntity, bool>> filter
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>()).AsNoTracking();

            return await query
                .Where(filter)
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDtoIncludingDeletedAsync(
            Expression<Func<TEntity, bool>> filter
        )
        {
            var query = SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.IncludeDeleted
                )
                .AsNoTracking();

            return await query
                .Where(filter)
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDeletedDtoAsync()
        {
            return await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.DeletedOnly
                )
                .AsNoTracking()
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDeletedDtoAsync(
            Expression<Func<TEntity, bool>> filter
        )
        {
            var query = SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.DeletedOnly
                )
                .AsNoTracking();

            return await query
                .Where(filter)
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(IApiPagination apiPagination)
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>()).AsNoTracking();

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(
                apiPagination,
                _paginationProcessor,
                _mapper
            );
        }

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>())
                .AsNoTracking()
                .Where(filter);

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(
                apiPagination,
                _paginationProcessor,
                _mapper
            );
        }

        public virtual async Task<TDto?> FindDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            bool throwOnMultipleFound = true,
            bool ignoreDeletedWithAuditable = true
        )
        {
            var query = SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    ignoreDeletedWithAuditable
                )
                .AsNoTracking()
                .Where(filter)
                .ProjectTo<TDto>(_mapper.ConfigurationProvider);

            if (throwOnMultipleFound)
            {
                return await query.SingleOrDefaultAsync();
            }
            else
            {
                return await query.FirstOrDefaultAsync();
            }
        }

        public virtual async Task<TDto> GetByIdDtoAsync(TIdType id)
        {
            var dto = await SetupQueryModifications(_dbContext.Set<TEntity>())
                .AsNoTracking()
                .Where(e => e.Id.Equals(id))
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (dto == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), id);
            }

            return dto;
        }

        public virtual async Task<TDto> GetByIdDtoIncludingDeletedAsync(TIdType id)
        {
            var dto = await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.IncludeDeleted
                )
                .AsNoTracking()
                .Where(e => e.Id.Equals(id))
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (dto == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), id);
            }

            return dto;
        }

        public virtual async Task UpdateAsync(TInputDto item)
        {
            var dbEntity = await GetByIdAsync(item.Id);
            _mapper.Map(item, dbEntity);

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
    }

    public abstract class BaseCrudDtoRepo<TEntity, TIdType, TDto>
        : BaseCrudDtoRepo<TEntity, TIdType, TDto, TDto>,
            ICrudDtoRepo<TEntity, TIdType, TDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
    {
        protected BaseCrudDtoRepo(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor
        )
            : base(dbContext, mapper, paginationProcessor) { }
    }
}
