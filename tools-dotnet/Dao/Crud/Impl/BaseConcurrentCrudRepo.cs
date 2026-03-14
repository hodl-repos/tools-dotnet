using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

        public override async Task UpdateAsync(
            TEntity item,
            CancellationToken cancellationToken = default
        )
        {
            var concurrencyToken =
                CrudConcurrencyHelper.GetRequiredRequestConcurrencyToken<TConcurrencyToken>(
                    _concurrencyConfiguration,
                    item
                );

            await UpdateAsync(item, concurrencyToken, cancellationToken);
        }

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

        public override Task RemoveAsync(TIdType id, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                $"Use {nameof(RemoveAsync)}({nameof(id)}, concurrencyToken) on concurrency-aware repos."
            );
        }

        public override Task RestoreAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            throw new InvalidOperationException(
                $"Use {nameof(RestoreAsync)}({nameof(id)}, concurrencyToken) on concurrency-aware repos."
            );
        }

        public override Task HardRemoveAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            throw new InvalidOperationException(
                $"Use {nameof(HardRemoveAsync)}({nameof(id)}, concurrencyToken) on concurrency-aware repos."
            );
        }

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

        public virtual async Task RestoreAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await GetByIdInternalAsync(id, false, cancellationToken);
            CrudConcurrencyHelper.EnsureMatchingConcurrencyTokenValue(
                _concurrencyConfiguration,
                entity,
                concurrencyToken
            );

            if (entity is not IAuditableEntity auditableEntity)
            {
                throw CreateSoftDeleteNotSupportedException(nameof(RestoreAsync));
            }

            if (auditableEntity.DeletedTimestamp == null)
            {
                return;
            }

            auditableEntity.DeletedTimestamp = null;
            _dbContext.Attach(auditableEntity);
            _dbContext.Entry(auditableEntity).State = EntityState.Modified;

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
            var entity = await GetByIdInternalAsync(id, false, cancellationToken);
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

        public virtual async Task<TConcurrencyToken> GetConcurrencyTokenAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await SetupQueryModifications(_dbContext.Set<TEntity>(), false)
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
    }
}
