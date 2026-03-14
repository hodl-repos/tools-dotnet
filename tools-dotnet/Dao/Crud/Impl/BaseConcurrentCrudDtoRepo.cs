using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;
using tools_dotnet.Paging;
using tools_dotnet.Utility;

namespace tools_dotnet.Dao.Crud.Impl
{
    public abstract class BaseConcurrentCrudDtoRepo<
        TEntity,
        TIdType,
        TDto,
        TInputDto,
        TConcurrencyToken
    > : BaseConcurrentCrudRepo<TEntity, TIdType, TConcurrencyToken>,
            IConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TInputDto, TConcurrencyToken>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TInputDto : IDtoWithId<TIdType>
    {
        protected BaseConcurrentCrudDtoRepo(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor,
            CrudConcurrencyConfiguration concurrencyConfiguration
        )
            : base(dbContext, mapper, paginationProcessor, concurrencyConfiguration) { }

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

            return await query.FirstOrDefaultAsync();
        }

        public virtual async Task<TDto> GetByIdDtoAsync(TIdType id)
        {
            var dto = await SetupQueryModifications(_dbContext.Set<TEntity>(), false)
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
            var concurrencyToken =
                CrudConcurrencyHelper.GetRequiredRequestConcurrencyToken<TConcurrencyToken>(
                    _concurrencyConfiguration,
                    item
                );

            await UpdateAsync(item, concurrencyToken);
        }

        public virtual async Task UpdateAsync(TInputDto item, TConcurrencyToken concurrencyToken)
        {
            var dbEntity = await GetByIdAsync(item.Id);
            CrudConcurrencyHelper.EnsureMatchingConcurrencyTokenValue(
                _concurrencyConfiguration,
                dbEntity,
                concurrencyToken
            );

            _mapper.Map(item, dbEntity);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw await CrudConcurrencyHelper.CreateConcurrentModificationExceptionAsync(
                    _concurrencyConfiguration,
                    ex,
                    concurrencyToken
                );
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is PostgresException pgEx)
                {
                    switch (pgEx.SqlState)
                    {
                        case PostgresErrorCodes.ForeignKeyViolation:
                            throw new DependentItemException(pgEx.Message, false);
                        case PostgresErrorCodes.UniqueViolation:
                            throw new ConflictingItemException(pgEx.Message);
                    }
                }

                throw;
            }
        }
    }

    public abstract class BaseConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TConcurrencyToken>
        : BaseConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TDto, TConcurrencyToken>,
            IConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TConcurrencyToken>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
    {
        protected BaseConcurrentCrudDtoRepo(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor,
            CrudConcurrencyConfiguration concurrencyConfiguration
        )
            : base(dbContext, mapper, paginationProcessor, concurrencyConfiguration) { }
    }
}
