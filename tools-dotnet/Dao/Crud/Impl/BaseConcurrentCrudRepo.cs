using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Dao.Crud.Impl
{
    public abstract class BaseConcurrentCrudRepo<TEntity, TIdType, TConcurrencyToken>
        : BaseCrudRepo<TEntity, TIdType>,
            IConcurrentCrudRepo<TEntity, TIdType, TConcurrencyToken>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
    {
        protected readonly CrudConcurrencyConfiguration _concurrencyConfiguration;

        protected BaseConcurrentCrudRepo(
            DbContext dbContext,
            IMapper mapper,
            IPaginationProcessor paginationProcessor,
            CrudConcurrencyConfiguration concurrencyConfiguration
        )
            : base(dbContext, mapper, paginationProcessor)
        {
            _concurrencyConfiguration =
                concurrencyConfiguration
                ?? throw new ArgumentNullException(nameof(concurrencyConfiguration));
        }

        public override async Task UpdateAsync(TEntity item)
        {
            var concurrencyToken =
                CrudConcurrencyHelper.GetRequiredRequestConcurrencyToken<TConcurrencyToken>(
                    _concurrencyConfiguration,
                    item
                );

            await UpdateAsync(item, concurrencyToken);
        }

        public virtual async Task UpdateAsync(TEntity item, TConcurrencyToken concurrencyToken)
        {
            var dbEntity = await GetByIdInternalAsync(item.Id);
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

        public override Task RemoveAsync(TIdType id)
        {
            throw new InvalidOperationException(
                $"Use {nameof(RemoveAsync)}({nameof(id)}, concurrencyToken) on concurrency-aware repos."
            );
        }

        public virtual async Task RemoveAsync(TIdType id, TConcurrencyToken concurrencyToken)
        {
            var entity = await GetByIdInternalAsync(id);
            CrudConcurrencyHelper.EnsureMatchingConcurrencyTokenValue(
                _concurrencyConfiguration,
                entity,
                concurrencyToken
            );

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
                if (
                    ex.InnerException is PostgresException pgEx
                    && pgEx.SqlState == PostgresErrorCodes.ForeignKeyViolation
                )
                {
                    throw new DependentItemException(pgEx.Message, true);
                }

                throw;
            }
        }

        public virtual async Task<TConcurrencyToken> GetConcurrencyTokenAsync(TIdType id)
        {
            var entity = await SetupQueryModifications(_dbContext.Set<TEntity>(), false)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id.Equals(id));

            if (entity == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), id);
            }

            return CrudConcurrencyHelper.GetRequiredPersistedConcurrencyToken<TConcurrencyToken>(
                _concurrencyConfiguration,
                entity
            );
        }
    }
}
