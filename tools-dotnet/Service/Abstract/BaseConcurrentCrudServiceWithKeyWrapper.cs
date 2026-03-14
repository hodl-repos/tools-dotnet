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

        public virtual async Task RemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken
        )
        {
            await _baseRepo.RemoveAsync(keyWrapper, concurrencyToken);
        }
    }
}
