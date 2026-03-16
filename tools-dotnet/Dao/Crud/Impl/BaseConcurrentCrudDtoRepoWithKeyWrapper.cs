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
    public abstract class BaseConcurrentCrudDtoRepoWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TInputDto,
        TConcurrencyToken
    > : BaseConcurrentCrudRepoWithKeyWrapper<TEntity, TKeyWrapper, TConcurrencyToken>,
            IConcurrentCrudDtoRepoWithKeyWrapper<
                TEntity,
                TKeyWrapper,
                TDto,
                TInputDto,
                TConcurrencyToken
            >
        where TEntity : class, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
        where TInputDto : class
    {
        protected BaseConcurrentCrudDtoRepoWithKeyWrapper(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor,
            CrudConcurrencyConfiguration concurrencyConfiguration
        )
            : base(dbContext, mapper, paginationProcessor, concurrencyConfiguration) { }

        public virtual async Task<TKeyWrapper> AddAsync(
            TKeyWrapper keyWrapper,
            TInputDto item,
            CancellationToken cancellationToken = default
        )
        {
            var entity = _mapper.Map<TEntity>(item);

            return await AddAsync(keyWrapper, entity, cancellationToken);
        }

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

        public virtual async Task<IEnumerable<TDto>> GetAllDtoAsync(
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        )
        {
            return await SetupQueryModifications(_dbContext.Set<TEntity>(), SoftDeleteQueryMode.ActiveOnly)
                .Where(filter)
                .AsNoTracking()
                .ProjectTo<TDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

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

        public virtual async Task<IPagedList<TDto>> GetAllDtoAsync(
            IApiPagination apiPagination,
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default
        )
        {
            var query = SetupQueryModifications(_dbContext.Set<TEntity>(), SoftDeleteQueryMode.ActiveOnly)
                .Where(filter)
                .AsNoTracking();

            return await query.SortFilterAndPageWithProjectToAsync<TEntity, TDto>(
                apiPagination,
                _paginationProcessor,
                _mapper,
                cancellationToken: cancellationToken
            );
        }

        public virtual async Task<TDto> GetByIdDtoAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            var dto = await SetupQueryModifications(
                    _dbContext.Set<TEntity>(),
                    SoftDeleteQueryMode.ActiveOnly
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

        public virtual async Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TInputDto item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            var dbEntity = await GetByIdInternalAsync(
                keyWrapper,
                cancellationToken: cancellationToken
            );
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
    }

    public abstract class BaseConcurrentCrudDtoRepoWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TConcurrencyToken
    > : BaseConcurrentCrudDtoRepoWithKeyWrapper<
            TEntity,
            TKeyWrapper,
            TDto,
            TDto,
            TConcurrencyToken
        >,
            IConcurrentCrudDtoRepoWithKeyWrapper<
                TEntity,
                TKeyWrapper,
                TDto,
                TConcurrencyToken
            >
        where TEntity : class, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
    {
        protected BaseConcurrentCrudDtoRepoWithKeyWrapper(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor,
            CrudConcurrencyConfiguration concurrencyConfiguration
        )
            : base(dbContext, mapper, paginationProcessor, concurrencyConfiguration) { }
    }
}
