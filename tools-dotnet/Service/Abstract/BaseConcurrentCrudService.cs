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

        public virtual async Task<TConcurrencyToken> GetConcurrencyTokenAsync(TIdType id)
        {
            return await _baseRepo.GetConcurrencyTokenAsync(id);
        }

        public override async Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync()
        {
            return await _baseRepo.GetAllDtoIncludingDeletedAsync();
        }

        public override async Task<IEnumerable<TDto>> GetAllDeletedAsync()
        {
            return await _baseRepo.GetAllDeletedDtoAsync();
        }

        public override async Task<TDto> GetByIdIncludingDeletedAsync(TIdType id)
        {
            return await _baseRepo.GetByIdDtoIncludingDeletedAsync(id);
        }

        public virtual async Task UpdateAsync(TDto item, TConcurrencyToken concurrencyToken)
        {
            await _validator.ValidateAndThrowAsync(item);
            await _baseRepo.UpdateAsync(item, concurrencyToken);
        }

        public override Task RemoveAsync(TIdType id)
        {
            throw new System.InvalidOperationException(
                $"Use {nameof(RemoveAsync)}({nameof(id)}, concurrencyToken) on concurrency-aware services."
            );
        }

        public override Task RestoreAsync(TIdType id)
        {
            throw new System.InvalidOperationException(
                $"Use {nameof(RestoreAsync)}({nameof(id)}, concurrencyToken) on concurrency-aware services."
            );
        }

        public override Task HardRemoveAsync(TIdType id)
        {
            throw new System.InvalidOperationException(
                $"Use {nameof(HardRemoveAsync)}({nameof(id)}, concurrencyToken) on concurrency-aware services."
            );
        }

        public virtual async Task RemoveAsync(TIdType id, TConcurrencyToken concurrencyToken)
        {
            await _baseRepo.RemoveAsync(id, concurrencyToken);
        }

        public virtual async Task RestoreAsync(TIdType id, TConcurrencyToken concurrencyToken)
        {
            await _baseRepo.RestoreAsync(id, concurrencyToken);
        }

        public virtual async Task HardRemoveAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken
        )
        {
            await _baseRepo.HardRemoveAsync(id, concurrencyToken);
        }
    }
}
