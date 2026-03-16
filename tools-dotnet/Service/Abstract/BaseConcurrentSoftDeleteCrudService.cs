using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;

namespace tools_dotnet.Service.Abstract
{
    public abstract class BaseConcurrentSoftDeleteCrudService<
        TEntity,
        TIdType,
        TDto,
        TConcurrencyToken,
        TRepo,
        TValidator
    > : BaseConcurrentCrudService<TEntity, TIdType, TDto, TConcurrencyToken, TRepo, TValidator>,
            IConcurrentSoftDeleteCrudService<TDto, TIdType, TConcurrencyToken>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TRepo : IConcurrentSoftDeleteCrudDtoRepo<
            TEntity,
            TIdType,
            TDto,
            TConcurrencyToken
        >
        where TValidator : IValidator<TDto>
    {
        protected BaseConcurrentSoftDeleteCrudService(
            IMapper mapper,
            TRepo baseRepo,
            TValidator validator
        )
            : base(mapper, baseRepo, validator) { }

        public virtual async Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDeletedAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(
                SoftDeleteQueryMode.DeletedOnly,
                cancellationToken
            );
        }

        public virtual async Task<TDto> GetByIdIncludingDeletedAsync(
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
