using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dto;

namespace tools_dotnet.Service.Abstract
{
    public abstract class BaseConcurrentCrudServiceWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TConcurrencyToken,
        TRepo,
        TValidator
    > : BaseCrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto, TRepo, TValidator>,
            IConcurrentCrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto, TConcurrencyToken>
        where TEntity : class, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class, IDto
        where TRepo : IConcurrentCrudDtoRepoWithKeyWrapper<
            TEntity,
            TKeyWrapper,
            TDto,
            TConcurrencyToken
        >
        where TValidator : IValidator<TDto>
    {
        protected BaseConcurrentCrudServiceWithKeyWrapper(
            IMapper mapper,
            TRepo baseRepo,
            TValidator validator
        )
            : base(mapper, baseRepo, validator) { }

        public virtual async Task<TConcurrencyToken> GetConcurrencyTokenAsync(TKeyWrapper keyWrapper)
        {
            return await _baseRepo.GetConcurrencyTokenAsync(keyWrapper);
        }

        public override async Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(TKeyWrapper keyWrapper)
        {
            return await _baseRepo.GetAllDtoIncludingDeletedAsync(
                keyWrapper.GetContainingResourceFilter()
            );
        }

        public override async Task<IEnumerable<TDto>> GetAllDeletedAsync(TKeyWrapper keyWrapper)
        {
            return await _baseRepo.GetAllDeletedDtoAsync(keyWrapper.GetContainingResourceFilter());
        }

        public override async Task<TDto> GetByIdIncludingDeletedAsync(TKeyWrapper keyWrapper)
        {
            return await _baseRepo.GetByIdDtoIncludingDeletedAsync(keyWrapper);
        }

        public virtual async Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TDto item,
            TConcurrencyToken concurrencyToken
        )
        {
            await SetAndValidateKeyAsync(item, keyWrapper);
            await _validator.ValidateAndThrowAsync(item);
            await _baseRepo.UpdateAsync(keyWrapper, item, concurrencyToken);
        }

        public override Task RemoveAsync(TKeyWrapper keyWrapper)
        {
            throw new System.InvalidOperationException(
                $"Use {nameof(RemoveAsync)}({nameof(keyWrapper)}, concurrencyToken) on concurrency-aware services."
            );
        }

        public override Task RestoreAsync(TKeyWrapper keyWrapper)
        {
            throw new System.InvalidOperationException(
                $"Use {nameof(RestoreAsync)}({nameof(keyWrapper)}, concurrencyToken) on concurrency-aware services."
            );
        }

        public override Task HardRemoveAsync(TKeyWrapper keyWrapper)
        {
            throw new System.InvalidOperationException(
                $"Use {nameof(HardRemoveAsync)}({nameof(keyWrapper)}, concurrencyToken) on concurrency-aware services."
            );
        }

        public virtual async Task RemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken
        )
        {
            await _baseRepo.RemoveAsync(keyWrapper, concurrencyToken);
        }

        public virtual async Task RestoreAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken
        )
        {
            await _baseRepo.RestoreAsync(keyWrapper, concurrencyToken);
        }

        public virtual async Task HardRemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken
        )
        {
            await _baseRepo.HardRemoveAsync(keyWrapper, concurrencyToken);
        }
    }
}
