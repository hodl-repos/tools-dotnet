using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;

namespace tools_dotnet.Service.Abstract
{
    public abstract class BaseConcurrentCrudService<
        TEntity,
        TIdType,
        TDto,
        TConcurrencyToken,
        TRepo,
        TValidator
    > : BaseCrudService<TEntity, TIdType, TDto, TRepo, TValidator>,
            IConcurrentCrudService<TDto, TIdType, TConcurrencyToken>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TRepo : IConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TConcurrencyToken>
        where TValidator : IValidator<TDto>
    {
        protected BaseConcurrentCrudService(IMapper mapper, TRepo baseRepo, TValidator validator)
            : base(mapper, baseRepo, validator) { }

        public virtual async Task<TConcurrencyToken> GetConcurrencyTokenAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetConcurrencyTokenAsync(id, cancellationToken);
        }

        public override async Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );
        }

        public override async Task<IEnumerable<TDto>> GetAllDeletedAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(
                SoftDeleteQueryMode.DeletedOnly,
                cancellationToken
            );
        }

        public override async Task<TDto> GetByIdIncludingDeletedAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetByIdDtoAsync(
                id,
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );
        }

        public virtual async Task UpdateAsync(
            TDto item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            await _validator.ValidateAndThrowAsync(item, cancellationToken);
            await _baseRepo.UpdateAsync(item, concurrencyToken, cancellationToken);
        }

        public override Task RemoveAsync(TIdType id, CancellationToken cancellationToken = default)
        {
            throw new System.InvalidOperationException(
                $"Use {nameof(RemoveAsync)}({nameof(id)}, concurrencyToken) on concurrency-aware services."
            );
        }

        public override Task RestoreAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            throw new System.InvalidOperationException(
                $"Use {nameof(RestoreAsync)}({nameof(id)}, concurrencyToken) on concurrency-aware services."
            );
        }

        public override Task HardRemoveAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            throw new System.InvalidOperationException(
                $"Use {nameof(HardRemoveAsync)}({nameof(id)}, concurrencyToken) on concurrency-aware services."
            );
        }

        public virtual async Task RemoveAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RemoveAsync(id, concurrencyToken, cancellationToken);
        }

        public virtual async Task RestoreAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RestoreAsync(id, concurrencyToken, cancellationToken);
        }

        public virtual async Task HardRemoveAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.HardRemoveAsync(id, concurrencyToken, cancellationToken);
        }
    }
}
