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
    public abstract class BaseConcurrentSoftDeleteCrudServiceWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TConcurrencyToken,
        TRepo,
        TValidator
    > : BaseConcurrentCrudServiceWithKeyWrapper<
            TEntity,
            TKeyWrapper,
            TDto,
            TConcurrencyToken,
            TRepo,
            TValidator
        >,
            IConcurrentSoftDeleteCrudServiceWithKeyWrapper<
                TEntity,
                TKeyWrapper,
                TDto,
                TConcurrencyToken
            >
        where TEntity : class, IAuditableEntity, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class, IDto
        where TRepo : IConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<
            TEntity,
            TKeyWrapper,
            TDto,
            TConcurrencyToken
        >
        where TValidator : IValidator<TDto>
    {
        protected BaseConcurrentSoftDeleteCrudServiceWithKeyWrapper(
            IMapper mapper,
            TRepo baseRepo,
            TValidator validator
        )
            : base(mapper, baseRepo, validator) { }

        public virtual async Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(
                keyWrapper.GetContainingResourceFilter(),
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDeletedAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(
                keyWrapper.GetContainingResourceFilter(),
                SoftDeleteQueryMode.DeletedOnly,
                cancellationToken
            );
        }

        public virtual async Task<TDto> GetByIdIncludingDeletedAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetByIdDtoAsync(
                keyWrapper,
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );
        }

        public virtual async Task RestoreAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RestoreAsync(keyWrapper, concurrencyToken, cancellationToken);
        }

        public virtual async Task HardRemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.HardRemoveAsync(keyWrapper, concurrencyToken, cancellationToken);
        }
    }
}
