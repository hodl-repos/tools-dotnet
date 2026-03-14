using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Services;

namespace tools_dotnet.Dao.Crud.Impl
{
    public abstract class BaseConcurrentCrudRepoWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TConcurrencyToken
    > : BaseCrudRepoWithKeyWrapper<TEntity, TKeyWrapper>,
            IConcurrentCrudRepoWithKeyWrapper<TEntity, TKeyWrapper, TConcurrencyToken>
        where TEntity : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        protected readonly CrudConcurrencyConfiguration _concurrencyConfiguration;

        protected BaseConcurrentCrudRepoWithKeyWrapper(
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

        public override async Task UpdateAsync(TKeyWrapper keyWrapper, TEntity item)
        {
            var concurrencyToken =
                CrudConcurrencyHelper.GetRequiredRequestConcurrencyToken<TConcurrencyToken>(
                    _concurrencyConfiguration,
                    item
                );

            await UpdateAsync(keyWrapper, item, concurrencyToken);
        }

        public virtual async Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TEntity item,
            TConcurrencyToken concurrencyToken
        )
        {
            var dbEntity = await GetByIdInternalAsync(keyWrapper);
            CrudConcurrencyHelper.EnsureMatchingConcurrencyTokenValue(
                _concurrencyConfiguration,
                dbEntity,
                concurrencyToken
            );

            _mapper.Map(item, dbEntity);
            keyWrapper.UpdateEntityWithContainingResource(dbEntity);

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
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, false);
                throw;
            }
        }

        public override Task RemoveAsync(TKeyWrapper keyWrapper)
        {
            throw new InvalidOperationException(
                $"Use {nameof(RemoveAsync)}({nameof(keyWrapper)}, concurrencyToken) on concurrency-aware repos."
            );
        }

        public override Task RestoreAsync(TKeyWrapper keyWrapper)
        {
            throw new InvalidOperationException(
                $"Use {nameof(RestoreAsync)}({nameof(keyWrapper)}, concurrencyToken) on concurrency-aware repos."
            );
        }

        public override Task HardRemoveAsync(TKeyWrapper keyWrapper)
        {
            throw new InvalidOperationException(
                $"Use {nameof(HardRemoveAsync)}({nameof(keyWrapper)}, concurrencyToken) on concurrency-aware repos."
            );
        }

        public virtual async Task RemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken
        )
        {
            var entity = await GetByIdInternalAsync(keyWrapper);
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
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, true);
                throw;
            }
        }

        public virtual async Task RestoreAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken
        )
        {
            var entity = await GetByIdInternalAsync(keyWrapper, false);
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
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, false);
                throw;
            }
        }

        public virtual async Task HardRemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken
        )
        {
            var entity = await GetByIdInternalAsync(keyWrapper, false);
            CrudConcurrencyHelper.EnsureMatchingConcurrencyTokenValue(
                _concurrencyConfiguration,
                entity,
                concurrencyToken
            );

            _dbContext.Remove(entity);

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
                CrudDbUpdateExceptionTranslator.ThrowIfKnown(ex, true);
                throw;
            }
        }

        public virtual async Task<TConcurrencyToken> GetConcurrencyTokenAsync(TKeyWrapper keyWrapper)
        {
            var entity = await SetupQueryModifications(_dbContext.Set<TEntity>(), false)
                .AsNoTracking()
                .FirstOrDefaultAsync(keyWrapper.GetKeyFilter());

            if (entity == null)
            {
                throw ItemNotFoundException.Create(nameof(TEntity), keyWrapper.GetKeyAsString());
            }

            return CrudConcurrencyHelper.GetRequiredPersistedConcurrencyToken<TConcurrencyToken>(
                _concurrencyConfiguration,
                entity
            );
        }
    }
}
