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
    public abstract class BaseCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TInputDto>
        : BaseCrudRepoWithKeyWrapper<TEntity, TKeyWrapper>,
            ICrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TInputDto>
        where TEntity : class, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
        where TInputDto : class
    {
        protected BaseCrudDtoRepoWithKeyWrapper(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor
        )
            : base(dbContext, mapper, paginationProcessor) { }

        public virtual async Task<TKeyWrapper> AddAsync(TKeyWrapper keyWrapper, TInputDto item)
        {
            var entity = _mapper.Map<TEntity>(item);

            return await AddAsync(keyWrapper, entity);
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
            return await SetupQueryModifications(_dbContext.Set<TEntity>())
                .Where(filter)
                .AsNoTracking()
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDtoIncludingDeletedAsync(
            Expression<Func<TEntity, bool>> filter
        )
        {
            return await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.IncludeDeleted
                )
                .Where(filter)
                .AsNoTracking()
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
            return await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.DeletedOnly
                )
                .Where(filter)
                .AsNoTracking()
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
                .Where(filter)
                .AsNoTracking();

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(
                apiPagination,
                _paginationProcessor,
                _mapper
            );
        }

        public virtual async Task<TDto> GetByIdDtoAsync(TKeyWrapper keyWrapper)
        {
            var dto = await SetupQueryModifications(_dbContext.Set<TEntity>())
                .AsNoTracking()
                .Where(keyWrapper.GetKeyFilter())
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (dto == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), keyWrapper.GetKeyAsString());
            }

            return dto;
        }

        public virtual async Task<TDto> GetByIdDtoIncludingDeletedAsync(TKeyWrapper keyWrapper)
        {
            var dto = await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.IncludeDeleted
                )
                .AsNoTracking()
                .Where(keyWrapper.GetKeyFilter())
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (dto == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), keyWrapper.GetKeyAsString());
            }

            return dto;
        }

        public virtual async Task UpdateAsync(TKeyWrapper keyWrapper, TInputDto item)
        {
            var dbEntity = await GetByIdInternalAsync(keyWrapper);
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

    public abstract class BaseCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        : BaseCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TDto>,
            ICrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TEntity : class, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
    {
        protected BaseCrudDtoRepoWithKeyWrapper(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor
        )
            : base(dbContext, mapper, paginationProcessor) { }
    }
}
